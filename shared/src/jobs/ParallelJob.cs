using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteampunkDnD.Shared;

public partial class ParallelJob : Job
{
    private readonly IDictionary<Job, float> WeightedJobs;
    private readonly float TotalWeight;

    public ParallelJob(IDictionary<Job, float> weightedJobs)
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
        TotalWeight = weightedJobs.Values.Sum();
    }

    public override async Task Run()
    {
        var jobProgress = new Dictionary<Job, JobMetric>();

        await DeferredUtils.RunDeferred(() =>
        {
            foreach (var (job, weight) in WeightedJobs)
            {
                job.Updated += (wrapper) =>
                {
                    var metric = wrapper.Value;
                    jobProgress[job] = metric;
                    var newMetric = CalculateJobMetric(jobProgress);
                    var newWrapper = new GodotWrapper<JobMetric>(newMetric);
                    EmitSignal(SignalName.Updated, newWrapper);
                };
                job.Completed += () =>
                {
                    var metric = new JobMetric(1, job.SuccessMessage);
                    jobProgress[job] = metric;
                    var newMetric = CalculateJobMetric(jobProgress);
                    var newWrapper = new GodotWrapper<JobMetric>(newMetric);
                    EmitSignal(SignalName.Updated, newWrapper);
                };
            }
        });

        // Start jobs
        var jobTasks = WeightedJobs.Select(wj => wj.Key.Run());

        // Wait for jobs to complete
        try
        {
            await Task.WhenAll(jobTasks);
        }
        catch (Exception)
        {
            DeferredUtils.CallDeferred(() => EmitSignal(SignalName.Failed));
            throw;
        }

        DeferredUtils.CallDeferred(() => EmitSignal(SignalName.Completed));
    }

    private JobMetric CalculateJobMetric(Dictionary<Job, JobMetric> jobProgress)
    {
        float accumulatedProgress = 0;
        var metricMessages = new List<string>();
        foreach (var (job, metric) in jobProgress)
        {
            var weight = WeightedJobs[job];
            accumulatedProgress += weight / TotalWeight * metric.ProgressPercent;
            metricMessages.Add(metric.Description);
        }

        return new JobMetric(accumulatedProgress, string.Join(", ", metricMessages));
    }
}
