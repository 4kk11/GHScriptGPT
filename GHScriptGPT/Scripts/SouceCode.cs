using Grasshopper.GUI.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace GHScriptGPT.Scripts
{
    public class SouceCode
    {
        public string CodeText { get; private set; }
        public IEnumerable<string> CodeLines { get; private set; }
        public SouceCode(string codeText)
        {
            var lines = codeText.Split('\n');
            CodeText = codeText;
            CodeLines = lines;
        }

        public SouceCode(IEnumerable<string> codeLines)
        {
            var text = string.Join("\n", codeLines);
            CodeText = text;
            CodeLines = codeLines;
        }

        public SouceCode ExtractFunctionCode(string functionStart)
        {
            string text = CodeText;
            int functionStartIndex = text.IndexOf(functionStart);

            if (functionStartIndex == -1)
            {
                throw new Exception("Not found function start");
            }

            int openBraces = 0;
            bool codeBlockStarted = false;
            StringBuilder extracted = new StringBuilder();

            for (int i = functionStartIndex; i < text.Length; i++)
            {
                if (text[i] == '{')
                {
                    openBraces++;
                    if (!codeBlockStarted)
                    {
                        codeBlockStarted = true;
                        continue;  // Skip appending the first opening brace
                    }
                }
                else if (text[i] == '}')
                {
                    openBraces--;
                    if (openBraces == 0)
                    {
                        break;  // Stop processing after closing the outermost brace
                    }
                }

                if (codeBlockStarted)
                {
                    extracted.Append(text[i]);
                }
            }

            // Remove the closing brace and trim the result
            string result = extracted.ToString().TrimEnd('}');
            return new SouceCode(result);
        }


    }

}
