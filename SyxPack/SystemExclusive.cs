public interface ISystemExclusiveData
{
    // Gets the data of the SysEx message
    public List<byte> Data { get; }

    // Gets the length of the SysEx data in bytes
    public int DataLength { get; }
}
