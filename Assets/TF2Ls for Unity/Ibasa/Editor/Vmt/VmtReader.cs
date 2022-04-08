using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Ibasa.Valve.Vmt
{
    public sealed class VmtReader
    {
        private IEnumerable<string> Tokenize(TextReader reader)
        {
            int c;
            while ((c = reader.Read()) != -1)
            {
                while (char.IsWhiteSpace((char)c) && c != '\n') //want new lines
                {
                    c = reader.Read();
                }

                switch (c)
                {
                    case -1:
                        break;
                    case '/':
                        {
                            if (reader.Read() != '/')
                            {
                                throw new System.IO.InvalidDataException("Expected comment string.");
                            }

                            while (reader.Read() != '\n') { }

                            if ((c = reader.Peek()) == -1)
                            {
                                yield return null;
                            }
                            else
                            {
                                yield return Environment.NewLine;
                            }
                        }
                        break;
                    case '"':
                        {
                            StringBuilder builder = new StringBuilder();

                            while ((c = reader.Read()) != '"' && c != -1)
                            {
                                if (c == '\n')
                                {
                                    throw new System.IO.InvalidDataException("Newline in string.");
                                }
                                builder.Append((char)c);
                            }

                            if (c == -1)
                            {
                                throw new System.IO.InvalidDataException("Expected closing quote.");
                            }

                            yield return builder.ToString();
                        }
                        break;
                    case '{':
                        yield return "{"; break;
                    case '}':
                        yield return "}"; break;
                    case '\n':
                        yield return Environment.NewLine; break;
                    default:
                        {
                            StringBuilder builder = new StringBuilder();

                            do
                            {
                                builder.Append((char)c);
                            } while ((c = reader.Read()) != -1
                                && !char.IsWhiteSpace((char)c)
                                && c != '{' && c != '}' && c != '\n');

                            yield return builder.ToString();
                        }
                        break;
                }
            }
        }

        readonly TextReader Reader;

        public VmtReader(TextReader reader)
        {
            Reader = reader;
        }


    }
}