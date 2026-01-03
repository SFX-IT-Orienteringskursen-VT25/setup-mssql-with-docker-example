namespace SetupMssqlExample;

public interface IDatabase
{
    void Setup();
    void InsertValue(string value);
    void Select();
    void DeleteAll();
}