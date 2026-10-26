using cxed;
class Program
{
    static void Main(string[] args)
    {
        string filePath = args.Length > 0 ? args[0] : null;
        TextEditor editor = new TextEditor(filePath);
        editor.Run();
    }
}