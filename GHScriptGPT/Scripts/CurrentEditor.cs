using Grasshopper.GUI.Script;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
			// Get the code of the RunScript function.
			var lines = _codeBlocks[2].Lines;
			lines = lines.Skip(lines.Count() - 2);
			lines = lines.Concat(_codeBlocks[3].Lines);
			lines = lines.Append("}");
			lines = lines.Select(text => text.TrimStart());
			return new SouceCode(lines);
		}

		public void SetCode_RunScript(SouceCode code)
		{
			GH_CodeBlock newCodeBlock = new GH_CodeBlock();
			newCodeBlock.AddLines(code.CodeLines);
			_codeBlocks[3] = newCodeBlock;
			UpdateEditor();
		}

		private void UpdateEditor()
		{
			_editor.SetSourceCode(_codeBlocks);
		}

		public IEnumerable<string> GetErrors()
		{
			FieldInfo fieldInfo = _editor.GetType().GetField("m_objectId", BindingFlags.Instance | BindingFlags.NonPublic);
			Guid instanceGuid = (Guid)fieldInfo.GetValue(_editor);

			fieldInfo = _editor.GetType().GetField("m_document", BindingFlags.Instance | BindingFlags.NonPublic);
			GH_Document ghdoc = fieldInfo.GetValue(_editor) as GH_Document;

			IGH_DocumentObject docObject = ghdoc.FindObject(instanceGuid, true);

			fieldInfo = docObject.GetType().BaseType.GetField("_compilerErrors", BindingFlags.Instance | BindingFlags.NonPublic);
			var errors = fieldInfo.GetValue(docObject) as IEnumerable<string>;
			return errors;
		}
	}
}
