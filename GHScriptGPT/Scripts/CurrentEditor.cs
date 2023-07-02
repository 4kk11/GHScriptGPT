using Grasshopper.GUI.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GHScriptGPT.Scripts
{
	public class CurrentEditor
	{
		private GH_ScriptEditor _editor;
		private GH_CodeBlocks _codeBlocks;
		private CurrentEditor(GH_ScriptEditor editor)
		{
			_editor = editor;
			_codeBlocks = editor.GetSourceCode();
		}

		public static CurrentEditor GetCurrentEditor()
		{
			// todo: is current check
			var forms = System.Windows.Forms.Application.OpenForms;
			foreach (var form in forms)
			{
				if (form.GetType().Name != "GH_ScriptEditor") continue;
				GH_ScriptEditor editor = form as GH_ScriptEditor;
				return new CurrentEditor(editor);
			}
			return null;
		}

		public SouceCode GetCode_RunScript()
		{
			var lines = _codeBlocks[2].Lines;
			lines = lines.Skip(lines.Count() - 2);
			lines = lines.Concat(_codeBlocks[3].Lines);
			lines = lines.Append("}");
			lines = lines.Select(text => text.TrimStart());
			return new SouceCode(lines);
		}

		public void SetCode_RunScript(SouceCode code)
		{

			_codeBlocks[3].AddLines(code.CodeLines);
			UpdateEditor();
		}

		private void UpdateEditor()
		{
			_editor.SetSourceCode(_codeBlocks);
		}
	}
}
