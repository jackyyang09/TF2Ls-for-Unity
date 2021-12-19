using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Ibasa.Valve.Vmt
{
    public sealed class VmtWriter
    {
        readonly TextWriter Writer;

        int IndentLevel = 0;
        string IndentChars = "\t";

        StringBuilder Value = new StringBuilder();
        bool InValue = false;
        bool WroteMain = false;

        public VmtWriter(TextWriter writer)
        {
            Writer = writer;
        }

        public void Close()
        {
            if (IndentLevel != 0)
                throw new InvalidOperationException("This results in an invalid VMT document.");
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Is null or whitespace", "value");

            value = value.Trim('"', '\r', '\n', '\t', '\f', ' ');

            if (value.Contains('"') || value.Contains(Environment.NewLine))
                throw new ArgumentException("Cannot escape double quotes or newlines.", "value");

            if (value.Any((c) => char.IsWhiteSpace(c)))
                return string.Concat("\"", value, "\"");
            else
                return value;
        }

        private void WriteIndent()
        {
            for (int i = 0; i < IndentLevel; ++i)
            {
                Writer.Write(IndentChars);
            }
        }

        public void WriteStartGroup(string name)
        {
            if (WroteMain && IndentLevel == 0)
                throw new InvalidOperationException("This results in an invalid VMT document.");
            WroteMain = true;

            WriteIndent(); Writer.WriteLine(Escape(name));
            WriteIndent(); Writer.WriteLine("{");
            ++IndentLevel;
        }

        public void WriteEndGroup()
        {
            if (IndentLevel == 0)
                throw new InvalidOperationException("This results in an invalid VMT document.");
            if (InValue)
                throw new InvalidOperationException("This results in an invalid VMT document.");

            --IndentLevel;
            WriteIndent(); Writer.WriteLine("}");
        }

        public void WriteStartValue(string name)
        {
            if (IndentLevel == 0)
                throw new InvalidOperationException("This results in an invalid VMT document.");

            WriteIndent(); Writer.Write(Escape(name)); Writer.Write(" ");
            InValue = true;
        }

        public void WriteEndValue()
        {
            if (!InValue)
                throw new InvalidOperationException("This results in an invalid VMT document.");

            Writer.WriteLine(Escape(Value.ToString()));
            Value.Clear();
            InValue = false;
        }

        public void WriteValue(string value)
        {
            if (!InValue)
                throw new InvalidOperationException("This results in an invalid VMT document.");

            Value.Append(value);
        }
    }
}