using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteampunkDnD.Shared;

public partial class ConcurrentJob : Job
{
    private readonly IDictionary<Job, float> WeightedJobs;

    public ConcurrentJob(IDictionary<Job, float> weightedJobs)
    {
        WeightedJobs = weightedJobs;
        foreach (var job in weightedJobs.Keys)
        {
            if (job.IsInsideTree())
            {
                Logger.Singleton.Log(LogLevel.Warning, "Passed Job instance is already in SceneTree");
                continue;
            }
            AddChild(job);
        }
    }

    public override async Task Run()
    {
        float totalWeight = WeightedJobs.Values.Sum();
        float completedWeight = 0;
        foreach (var (job, weight) in WeightedJobs)
        {
            await DeferredUtils.RunDeferred(() =>
                job.Updated += (wrapper) =>
                {
                    // Notify about job update
                    var metric = wrapper.Value;
                    float percent = (metric.ProgressPercent * weight + completedWeight) / totalWeight;
                    var newMetric = new JobMetric(percent, metric.Description);
                    var newWrapper = new GodotWrapper<JobMetric>(newMetric);
                    EmitSignal(SignalName.Updated, newWrapper);
                });

            // Wait for job completion or fail
            try
            {
                await job.Run();
            }
            catch (Exception)
            {
                DeferredUtils.CallDeferred(() => EmitSignal(SignalName.Failed));
                throw;
            }

            completedWeight += weight;

            // Notify about job completion
            float percent = completedWeight / totalWeight;
            var newMetric = new JobMetric(percent, job.SuccessMessage);
            var newWrapper = new GodotWrapper<JobMetric>(newMetric);
            DeferredUtils.CallDeferred(() => EmitSignal(SignalName.Updated, newWrapper));
        }
        DeferredUtils.CallDeferred(() => EmitSignal(SignalName.Completed));
    }
}
