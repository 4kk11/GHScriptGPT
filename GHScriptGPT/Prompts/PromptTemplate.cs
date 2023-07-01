using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace GHScriptGPT.Prompts
{
	internal class PromptTemplate
	{
		private string _template;
		public PromptTemplate(string template)
		{
			this._template = template;
		}

		public static PromptTemplate FromFile(string filePath)
		{
			string template = File.ReadAllText(filePath);
			return new PromptTemplate(template);
		}

		public string FormatPrompt(Dictionary<string, string> replacements)
		{
			string result = _template;

			foreach (KeyValuePair<string, string> replacement in replacements)
			{
				result = Regex.Replace(result, @"\{" + replacement.Key + @"\}", replacement.Value);
			}

			return result;
		}

	}
}
