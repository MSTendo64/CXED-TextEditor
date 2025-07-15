class TextEditor
{
    private bool hasBeenSaved = false;
    private string originalFilePath;

    private List<string> lines = new List<string>();
    private string filePath;
    private bool modified = false;
    private int cursorLine = 0;
    private int cursorPos = 0;
    private string statusMessage = "";

    public TextEditor(string path)
    {
        filePath = path;
        originalFilePath = path;
        LoadFile();
    }

    private void LoadFile()
    {
        if (filePath != null)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    lines = File.ReadAllLines(filePath).ToList();
                    hasBeenSaved = true;
                }
                else
                {
                    string directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    lines.Add("");
                    hasBeenSaved = false;
                }
            }
            catch
            {
                Console.WriteLine("Error when opening the opening");
                Environment.Exit(1);
            }
        }
        else
        {
            lines.Add("");
        }
    }

    public void Run()
    {
        Console.Clear();

        while (true)
        {
            Redraw();
            try {
                var key = Console.ReadKey(true);
                HandleKey(key);
            }catch
            {
                Console.WriteLine();
            }

        }
    }

    private void Redraw()
    {
        Console.Clear();

        for (int i = 0; i < lines.Count; i++)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"{i + 1,4} ");
            Console.ResetColor();
            Console.WriteLine(lines[i]);
        }

        // Статусная строка
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("\n[Ctrl+D] Save | [Ctrl+X] Exit");
        Console.Write(statusMessage);
        Console.ResetColor();

        statusMessage = "";
        try
        {
            Console.SetCursorPosition(cursorPos + 5, cursorLine);
        }
        catch { }
        
    }

    private void HandleKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                if (cursorLine > 0) cursorLine--;
                AdjustCursorPosition();
                break;

            case ConsoleKey.DownArrow:
                if (cursorLine < lines.Count - 1) cursorLine++;
                AdjustCursorPosition();
                break;

            case ConsoleKey.LeftArrow:
                if (cursorPos > 0) cursorPos--;
                break;

            case ConsoleKey.RightArrow:
                if (cursorPos < lines[cursorLine].Length) cursorPos++;
                break;

            case ConsoleKey.Backspace:
                HandleBackspace();
                break;

            case ConsoleKey.Enter:
                HandleEnter();
                break;

            case ConsoleKey.Tab:
                InsertString("    "); // 4 spaces
                break;

            case ConsoleKey.D when key.Modifiers == ConsoleModifiers.Control:
                SaveFile();
                break;

            case ConsoleKey.X when key.Modifiers == ConsoleModifiers.Control:
                if (CheckExit()) { Console.Clear(); 
                    Environment.Exit(0); }
                break;

            default:
                if (!char.IsControl(key.KeyChar))
                {
                    InsertChar(key.KeyChar);
                }
                break;
        }
    }

    private void AdjustCursorPosition()
    {
        cursorPos = Math.Min(lines[cursorLine].Length, cursorPos);
    }

    private void HandleBackspace()
    {
        if (cursorPos > 0)
        {
            lines[cursorLine] = lines[cursorLine].Remove(cursorPos - 1, 1);
            cursorPos--;
            modified = true;
        }
        else if (cursorLine > 0)
        {
            cursorPos = lines[cursorLine - 1].Length;
            lines[cursorLine - 1] += lines[cursorLine];
            lines.RemoveAt(cursorLine);
            cursorLine--;
            modified = true;
        }
    }

    private void HandleEnter()
    {
        string left = lines[cursorLine].Substring(0, cursorPos);
        string right = lines[cursorLine].Substring(cursorPos);

        lines[cursorLine] = left;
        lines.Insert(cursorLine + 1, right);

        cursorLine++;
        cursorPos = 0;
        modified = true;
    }

    private void InsertChar(char c)
    {
        lines[cursorLine] = lines[cursorLine].Insert(cursorPos, c.ToString());
        cursorPos++;
        modified = true;
    }

    private void InsertString(string s)
    {
        lines[cursorLine] = lines[cursorLine].Insert(cursorPos, s);
        cursorPos += s.Length;
        modified = true;
    }

    private void SaveFile(bool force = false)
    {
        if (filePath == null || force)
        {
            if (!RequestFileName()) return;
            hasBeenSaved = false;
        }

        bool needConfirm = File.Exists(filePath) &&
                         !hasBeenSaved &&
                         filePath != originalFilePath;

        if (needConfirm && !ConfirmOverwrite())
        {
            statusMessage = "Save canceled";
            return;
        }

        try
        {
            File.WriteAllLines(filePath, lines);
            modified = false;
            hasBeenSaved = true;
            statusMessage = "The file has been saved successfully!";
        }
        catch
        {
            statusMessage = "File saving error!";
        }
    }
    private bool RequestFileName()
    {
        Console.ResetColor();
        Console.CursorVisible = true;

        try
        {
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.Write("Enter file name: ");
            string name = ReadLineWithCancel();

            if (string.IsNullOrWhiteSpace(name))
            {
                statusMessage = "Save canceled";
                return false;
            }

            filePath = Path.GetFullPath(name);
            return true;
        }
        finally
        {
            Console.CursorVisible = false;
        }
    }

    private bool ConfirmOverwrite()
    {
        Console.ResetColor();
        Console.CursorVisible = true;

        try
        {
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.Write($"File {Path.GetFileName(filePath)} exists. Overwrite? (Y/N) ");

            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
            } while (key.Key != ConsoleKey.Y && key.Key != ConsoleKey.N && key.Key != ConsoleKey.Escape);

            return key.Key == ConsoleKey.Y;
        }
        finally
        {
            Console.CursorVisible = false;
        }
    }
    private string ReadLineWithCancel()
    {
        string result = "";
        int pos = 0;

        while (true)
        {
            var key = Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    return result;

                case ConsoleKey.Escape:
                    Console.WriteLine("[canceled]");
                    return null;

                case ConsoleKey.Backspace when pos > 0:
                    result = result.Remove(pos - 1, 1);
                    pos--;
                    break;

                case ConsoleKey.LeftArrow when pos > 0:
                    pos--;
                    break;

                case ConsoleKey.RightArrow when pos < result.Length:
                    pos++;
                    break;

                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        result = result.Insert(pos, key.KeyChar.ToString());
                        pos++;
                    }
                    break;
            }

            Console.SetCursorPosition(17, Console.WindowHeight - 1);
            Console.Write(new string(' ', Console.WindowWidth - 17));
            Console.SetCursorPosition(17, Console.WindowHeight - 1);
            Console.Write(result);
            Console.SetCursorPosition(17 + pos, Console.WindowHeight - 1);
        }
    }

    private bool CheckExit()
    {
        if (!modified) return true;

        statusMessage = "There are unsaved changes! Get out? (Y/N)";
        Redraw();

        ConsoleKeyInfo key;
        do
        {
            key = Console.ReadKey(true);
        } while (key.Key != ConsoleKey.Y && key.Key != ConsoleKey.N);

        return key.Key == ConsoleKey.Y;
    }
}

class Program
{
    static void Main(string[] args)
    {
        string filePath = args.Length > 0 ? args[0] : null;
        TextEditor editor = new TextEditor(filePath);
        editor.Run();
    }
}