using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace GHScriptGPT.Prompts
{
	internal class PromptTemplate
	{
		private string _template;
		public PromptTemplate(string template)
		{
			if (template == null) throw new ArgumentNullException();
			this._template = template;
		}

		public static PromptTemplate FromFile(string fileName)
		{
			var assembly = Assembly.GetExecutingAssembly();
			string resourceName = assembly.GetManifestResourceNames().First(name => name.EndsWith(fileName));

			string template = null;
			using (Stream stream = assembly.GetManifestResourceStream(resourceName))
			{
				using (StreamReader reader = new StreamReader(stream))
				{ 
					template = reader.ReadToEnd();
				}
			}
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
