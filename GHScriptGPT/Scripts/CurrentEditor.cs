using Grasshopper.GUI.Script;
using Grasshopper.Kernel;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Drawing;
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
			//Check if only one editor is open
			var forms = System.Windows.Forms.Application.OpenForms;
			GH_ScriptEditor validEditor = null;
			
			foreach (var form in forms)
			{
				if (form.GetType().Name != "GH_ScriptEditor") continue;
				if (validEditor != null) // Already found one, therefore more than one exists
				{
					return null; // return null if more than one "GH_ScriptEditor" is found
				}
				validEditor = form as GH_ScriptEditor; // Assign the found form to validEditor
			}

			// If validEditor was assigned, return a new CurrentEditor instance. Otherwise, return null.
			return validEditor != null ? new CurrentEditor(validEditor) : null;
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
			/*
			GH_CodeBlock newCodeBlock = new GH_CodeBlock();
			newCodeBlock.AddLines(code.CodeLines);
			_codeBlocks[3] = newCodeBlock;
			UpdateEditor();
			*/
			InsertBlock(code.CodeText);
		}

		private void UpdateEditor()
		{
			_editor.SetSourceCode(_codeBlocks);
			
		}

		private void InsertBlock(string text)
		{

			BindingFlags bind = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

			// CodeEditor
			PropertyInfo pInfo_CodeEditor = _editor.GetType().GetProperty("CodeEditor", bind);
			object codeEditor = pInfo_CodeEditor.GetValue(_editor);
			Type type_codeEditor = typeof(Grasshopper.GUI.GH_AlignWidgetSettingsUI).Assembly.GetType("Grasshopper.GUI.GH_WndProcOverridenCodeEditor");


			string systemPath = Path.GetDirectoryName(typeof(Rhino.RhinoDoc).Assembly.Location);
			string assemblyPath = Path.Combine(systemPath, "QWhale.Editor.dll");
			Assembly assembly = Assembly.LoadFile(assemblyPath);


			// Selection
			PropertyInfo pInfo_Selection = type_codeEditor.GetProperty("Selection", bind);
			object selection = pInfo_Selection.GetValue(codeEditor);
			Type type_Selection = assembly.GetType("QWhale.Editor.Selection", true);

			// TextSource
			PropertyInfo pInfo_Source = type_codeEditor.GetProperty("Source", bind);
			object textSource = pInfo_Source.GetValue(codeEditor);
			Type type_TextSource = assembly.GetType("QWhale.Editor.TextSource.TextSource", true);

			// Position
			PropertyInfo pInfo_Position = type_TextSource.GetProperty("Position", bind);


			
			List<Rectangle> mutableRecs = GetMutableBlockRec(_codeBlocks);
			Rectangle rec = mutableRecs[1];

			// Position Change
			pInfo_Position.SetValue(textSource, new Point(rec.X, rec.Y));

			// Set Selection
			Type type_SelectionType = assembly.GetType("QWhale.Editor.SelectionType", true);
			MethodInfo mInfo_SetSelection = type_Selection.GetMethod("SetSelection", new Type[]{ type_SelectionType, typeof(Rectangle)});
			mInfo_SetSelection.Invoke(selection, new object[] { 1, rec });

			// Set Text
			MethodInfo mInfo_SetSelectedText = type_Selection.GetMethod("SetSelectedText", new Type[] { typeof(string), type_SelectionType, typeof(bool) });
			mInfo_SetSelectedText.Invoke(selection, new object[] { text, 1, true });

			// Smart Format
			MethodInfo mInfo_SmartFormatDocument = type_Selection.GetMethod("SmartFormatDocument", bind);
			mInfo_SmartFormatDocument.Invoke(selection, new object[] { });

			// Clear 
			MethodInfo mInfo_Clear = type_Selection.GetMethod("Clear", bind);
			mInfo_Clear.Invoke(selection, new object[] { });


		}

		private List<int[]> GetMutableIndex(bool[] isReadOnlys)
		{
			List<int[]> mutableIndexes = new List<int[]>();

			List<int> startIndexes = new List<int>();
			List<int> endIndexes = new List<int>();

			int count = isReadOnlys.Length;
			bool previous = false;

			for (int i = 0; i < count; i++)
			{
				bool isReadOnly = isReadOnlys[i];
				try
				{
					if (isReadOnly) continue;
					if (isReadOnlys[i + 1]) endIndexes.Add(i);
					if (!previous) continue;

					startIndexes.Add(i);

				}
				finally
				{
					previous = isReadOnly;
				}
			}

			for (int i = 0; i < startIndexes.Count; i++)
			{
				int[] start_end = new int[2];
				start_end[0] = startIndexes[i];
				start_end[1] = endIndexes[i];
				mutableIndexes.Add(start_end);
			}

			return mutableIndexes;
		}

		private List<Rectangle> GetMutableBlockRec(GH_CodeBlocks codeBlocks)
		{
			List<Rectangle> recs = new List<Rectangle>();

			string[] lines = new string[] { };
			bool[] isReadOnlys = new bool[] { };

			codeBlocks.GetAllLines(ref lines, ref isReadOnlys);

			List<int[]> mutableIndexes = GetMutableIndex(isReadOnlys);

			foreach (int[] start_end in mutableIndexes)
			{
				int start = start_end[0];
				int end = start_end[1];

				int height = end - start;

				int width = lines[end].Length;

				Rectangle rec = new Rectangle(0, start, width, height);
				recs.Add(rec);
			}

			return recs;
		}

		public IEnumerable<string> GetErrors()
		{
			IGH_DocumentObject owner = GetOwner();

			var fieldInfo = owner.GetType().BaseType.GetField("_compilerErrors", BindingFlags.Instance | BindingFlags.NonPublic);
			var errors = fieldInfo.GetValue(owner) as IEnumerable<string>;
			return errors;
		}

		public void RunScript()
		{
			IGH_DocumentObject owner = GetOwner();
			_editor.CacheCurrentScript();
			var methodInfo = owner.GetType().BaseType.GetMethod("SourceCodeChanged", BindingFlags.Instance | BindingFlags.Public);
			methodInfo.Invoke(owner, new object[] {_editor});
		}

		private IGH_DocumentObject GetOwner()
		{
			FieldInfo fieldInfo = _editor.GetType().GetField("m_objectId", BindingFlags.Instance | BindingFlags.NonPublic);
			Guid instanceGuid = (Guid)fieldInfo.GetValue(_editor);

			fieldInfo = _editor.GetType().GetField("m_document", BindingFlags.Instance | BindingFlags.NonPublic);
			GH_Document ghdoc = fieldInfo.GetValue(_editor) as GH_Document;

			IGH_DocumentObject docObject = ghdoc.FindObject(instanceGuid, true);
			return docObject;
		}
	}
}
