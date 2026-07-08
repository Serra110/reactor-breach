public interface ISaveable
{
    
    string GetUniqueId();

    
    string CaptureState();

    
    void RestoreState(string json);
}
