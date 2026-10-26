using System;
using System.Text;

namespace cxed
{
    public class TextEditor
    {
        private const int GutterWidth = 5;

        private bool hasBeenSaved = false;
        private string originalFilePath;

        private List<string> lines = new List<string>();
        private string filePath;
        private bool modified = false;
        private int cursorLine = 0;
        private int cursorPos = 0;
        private string statusMessage = "";

        private int topLine = 0;                 
        private int leftCol = 0;                 
        private bool fullRedraw = true;          
        private bool needClear = true;           
        private int dirtyMin = int.MaxValue; 
        private int dirtyMax = -1;
        private bool messageShown = false;       

        public TextEditor(string path)
        {
            filePath = path;
            originalFilePath = path;
            LoadFile();
        }

        public string[] ReadAllLines(string filepath)
        {
            List<string> lines = new List<string>();

            try
            {
                using FileStream stream = File.OpenRead(filepath);
                var sr = new StreamReader(stream);
                string line = sr.ReadLine();

                while (line != null)
                {
                    lines.Add(line);
                    line = sr.ReadLine();
                }
                stream.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return lines.ToArray();
        }

        private void LoadFile()
        {
            if (filePath != null)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        lines = new List<string>(ReadAllLines(filePath));
                        if (lines.Count == 0) lines.Add("");
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
                }
            }
            else
            {
                lines.Add("");
            }
        }

        bool isRunning = true;

        public void Run()
        {
            if (!isRunning)
            {
                Console.Clear();
                return;
            }

            while (isRunning)
            {
                Render();
                try
                {
                    var key = Console.ReadKey(true);
                    HandleKey(key);
                }
                catch
                {
                    Console.WriteLine();
                }
            }
        }


        private int VisibleLines()
        {
            int rows = Console.WindowHeight - 2;
            return rows < 1 ? 1 : rows;
        }

        private int TextWidth()
        {
            int w = Console.WindowWidth - 1 - GutterWidth;
            return w < 1 ? 1 : w;
        }

        private void EnsureCursorVisible()
        {
            int visible = VisibleLines();
            if (cursorLine < topLine) topLine = cursorLine;
            if (cursorLine >= topLine + visible) topLine = cursorLine - visible + 1;
            if (topLine < 0) topLine = 0;

            int width = TextWidth();
            if (cursorPos < leftCol) leftCol = cursorPos;
            if (cursorPos >= leftCol + width) leftCol = cursorPos - width + 1;
            if (leftCol < 0) leftCol = 0;
        }

        private void MarkDirty(int line)
        {
            if (line < dirtyMin) dirtyMin = line;
            if (line > dirtyMax) dirtyMax = line;
        }

        private void MarkDirtyFrom(int line)
        {
            MarkDirty(line);
            dirtyMax = int.MaxValue;
        }

        private void Render()
        {
            int oldTop = topLine;
            int oldLeft = leftCol;
            EnsureCursorVisible();
            if (topLine != oldTop || leftCol != oldLeft) fullRedraw = true;

            if (needClear)
            {
                Console.Clear();
                needClear = false;
                fullRedraw = true;
            }

            int visible = VisibleLines();

            if (fullRedraw)
            {
                for (int r = 0; r < visible; r++)
                    DrawLine(topLine + r);
                DrawHelp();
                fullRedraw = false;
            }
            else if (dirtyMax >= 0)
            {
                int from = Math.Max(dirtyMin, topLine);
                int to = (int)Math.Min((long)dirtyMax, (long)topLine + visible - 1);
                for (int i = from; i <= to; i++)
                    DrawLine(i);
            }

            dirtyMin = int.MaxValue;
            dirtyMax = -1;

            DrawMessage();
            PlaceCursor();
        }

        private void DrawLine(int index)
        {
            int screenRow = index - topLine;
            if (screenRow < 0 || screenRow >= VisibleLines()) return;

            Console.SetCursorPosition(0, screenRow);

            int width = TextWidth();

            if (index >= lines.Count)
            {
                Console.Write(new string(' ', Console.WindowWidth - 1));
                return;
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write((index + 1).ToString().PadLeft(4) + " ");
            Console.ResetColor();

            string text = lines[index];
            string visibleText = "";
            if (leftCol < text.Length)
            {
                visibleText = text.Substring(leftCol, Math.Min(width, text.Length - leftCol));
            }

            Console.Write(visibleText.PadRight(width));
        }

        private void DrawHelp()
        {
            Console.SetCursorPosition(0, Console.WindowHeight - 2);
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(Pad("[Ctrl+D] Save | [Ctrl+X] Exit"));
            Console.ResetColor();
        }

        private void DrawMessage()
        {
            if (statusMessage.Length == 0 && !messageShown) return;

            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(Pad(statusMessage));
            Console.ResetColor();

            messageShown = statusMessage.Length > 0;
            statusMessage = "";
        }

        private void PlaceCursor()
        {
            Console.SetCursorPosition(GutterWidth + cursorPos - leftCol, cursorLine - topLine);
        }

        private static string Pad(string s)
        {
            int width = Console.WindowWidth - 1;
            if (s.Length >= width) return s.Substring(0, width);
            return s.PadRight(width);
        }

        private void HandleKey(ConsoleKeyInfo key)
        {
            if (key.Modifiers == ConsoleModifiers.Control)
            {
                if (key.Key == ConsoleKey.D)
                {
                    SaveFile();
                    return;
                }
                if (key.Key == ConsoleKey.X)
                {
                    if (CheckExit())
                    {
                        Console.Clear();
                        isRunning = false;
                    }
                    else
                    {
                        needClear = true;
                    }
                    return;
                }
            }

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

                case ConsoleKey.Home:
                    cursorPos = 0;
                    break;

                case ConsoleKey.End:
                    cursorPos = lines[cursorLine].Length;
                    break;

                case ConsoleKey.PageUp:
                    cursorLine = Math.Max(0, cursorLine - VisibleLines());
                    AdjustCursorPosition();
                    break;

                case ConsoleKey.PageDown:
                    cursorLine = Math.Min(lines.Count - 1, cursorLine + VisibleLines());
                    AdjustCursorPosition();
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

                MarkDirty(cursorLine);
            }
            else if (cursorLine > 0)
            {
                cursorPos = lines[cursorLine - 1].Length;
                lines[cursorLine - 1] += lines[cursorLine];
                lines.RemoveAt(cursorLine);
                cursorLine--;
                modified = true;

                MarkDirtyFrom(cursorLine);
            }
        }

        private void HandleEnter()
        {
            string left = lines[cursorLine].Substring(0, cursorPos);
            string right = lines[cursorLine].Substring(cursorPos);

            lines[cursorLine] = left;
            lines.Insert(cursorLine + 1, right);

            MarkDirtyFrom(cursorLine);

            cursorLine++;
            cursorPos = 0;
            modified = true;
        }

        private void InsertChar(char c)
        {
            lines[cursorLine] = lines[cursorLine].Insert(cursorPos, c.ToString());
            cursorPos++;
            modified = true;

            MarkDirty(cursorLine);
        }

        private void InsertString(string s)
        {
            lines[cursorLine] = lines[cursorLine].Insert(cursorPos, s);
            cursorPos += s.Length;
            modified = true;

            MarkDirty(cursorLine);
        }
        private void SaveFile(bool force = false)
        {
            if (filePath == null || force)
            {
                if (!RequestFileName())
                {
                    needClear = true;
                    return;
                }
                hasBeenSaved = false;
            }

            bool needConfirm = File.Exists(filePath) &&
                             !hasBeenSaved &&
                             filePath != originalFilePath;

            if (needConfirm && !ConfirmOverwrite())
            {
                statusMessage = "Save canceled";
                needClear = true;
                return;
            }

            try
            {
                WriteAllLines(filePath, lines);
                modified = false;
                hasBeenSaved = true;
                statusMessage = "The file has been saved successfully!";
            }
            catch
            {
                statusMessage = "File saving error!";
            }

            needClear = true;
        }

        public static void WriteAllLines(string path, IEnumerable<string> contents)
        {
            WriteAllLines(path, contents, new UTF8Encoding(false));
        }

        public static void WriteAllLines(string path, string[] contents)
        {
            WriteAllLines(path, (IEnumerable<string>)contents, new UTF8Encoding(false));
        }

        public static void WriteAllLines(string path, IEnumerable<string> contents, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            CheckInvalidPathChars(path);

            if (File.Exists(path))
                File.Delete(path);

            using FileStream stream = File.Create(path);
            InternalWriteAllLines(new StreamWriter(stream, encoding), contents);
            stream.Close();
        }

        private static void CheckInvalidPathChars(string path)
        {
            if (path.Length == 0)
                throw new ArgumentException("Path cant be empty.", nameof(path));

            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                throw new ArgumentException("Path is incorrect.", nameof(path));
        }

        public static void WriteAllLines(string path, string[] contents, Encoding encoding)
        {
            WriteAllLines(path, (IEnumerable<string>)contents, encoding);
        }

        private static void InternalWriteAllLines(TextWriter writer, IEnumerable<string> contents)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));

            try
            {
                foreach (string line in contents)
                {
                    writer.WriteLine(line);
                }
            }
            finally
            {
                writer?.Dispose();
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
                        return result;

                    case ConsoleKey.Escape:
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
                Console.Write(new string(' ', Console.WindowWidth - 18));
                Console.SetCursorPosition(17, Console.WindowHeight - 1);
                Console.Write(result);
                Console.SetCursorPosition(17 + pos, Console.WindowHeight - 1);
            }
        }

        private bool CheckExit()
        {
            if (!modified) return true;

            statusMessage = "There are unsaved changes! Get out? (Y/N)";
            DrawMessage();

            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
            } while (key.Key != ConsoleKey.Y && key.Key != ConsoleKey.N);

            return key.Key == ConsoleKey.Y;
        }
    }
}
