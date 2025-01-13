namespace SteampunkDnD.Shared;

public readonly struct MarkedValue<TValue>
{
    public readonly uint Id;
    public readonly TValue Value;
    public MarkedValue(uint id, TValue value)
    {
        Id = id;
        Value = value;
    }

    public void Deconstruct(out uint id, out TValue value)
    {
        id = Id;
        value = Value;
    }
}
