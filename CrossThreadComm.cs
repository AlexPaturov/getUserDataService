namespace GetUserDataServiceGUI
{
  public class CrossThreadComm
  {
    public enum State
    {
      start,
      connect,
      disconnect,
      stop,
      terminate,
    }

    public delegate void TraceCb(object obj);

    public delegate void UpdateState(object obj, CrossThreadComm.State state);

    public delegate void UpdateRXTX(object obj, int bytesFromSerial, int bytesToSerial);
  }
}
