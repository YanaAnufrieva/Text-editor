﻿using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TextEditorMVC.Models;
using TextEditorMVC.poliz;
using TextEditorMVC.Text;

namespace TextEditorMVC
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		FilesController filesController = new FilesController();
		LexerController lexerController = new LexerController();
		ParserController parserController = new ParserController();
		ErrorNeutralizerController errorNeutralizerController = new ErrorNeutralizerController();
		public MainWindow()
		{
			InitializeComponent();

			TabControlFiles.Items.Clear();
			InitializeComboBoxFontSizes();

			TextBoxCurrentLanguage.Text = "Язык ввода: " + InputLanguageManager.Current.CurrentInputLanguage.DisplayName;

			System.Windows.Input.InputLanguageManager.Current.InputLanguageChanged += new InputLanguageEventHandler((sender, e) =>
			{
				TextBoxCurrentLanguage.Text = "Язык ввода: " + e.NewLanguage.DisplayName;
			});
		}

		private void CreateFile_Click(object sender, RoutedEventArgs e)
		{
			if (filesController.Files.Count > 0)
			{
				AskToSave();
			}

			if (filesController.CreateFile())
			{
				CreateNewTab(filesController.OpenedFile.FileName, filesController.OpenedFile.Content);
				TabControlFiles.SelectedIndex = TabControlFiles.Items.Count - 1;
			}
		}

		private void OpenFile_Click(object sender, RoutedEventArgs e)
		{
			if (filesController.Files.Count > 0)
			{
				AskToSave();
			}

			if (filesController.OpenFile())
			{
				CreateNewTab(filesController.OpenedFile.FileName, filesController.OpenedFile.Content);
				TabControlFiles.SelectedIndex = TabControlFiles.Items.Count - 1;
			}
		}

		private void SaveFile_Click(object sender, RoutedEventArgs e)
		{
			if (TabControlFiles.Items.Count == 0) return;

			TabItem currentItem = (TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex];
			String content = ((TextBox)currentItem.Content).Text;

			filesController.SaveFile();
		}

		private void SaveAsFile_Click(object sender, RoutedEventArgs e)
		{
			if (TabControlFiles.Items.Count == 0) return;

			TabItem currentItem = (TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex];

			String content = ((TextBox)currentItem.Content).Text;

			filesController.SaveAsFile();


			if (((TextBlock)((StackPanel)currentItem.Header).Children[0]).Text != filesController.OpenedFile.FileName)
			{
				((TextBlock)((StackPanel)currentItem.Header).Children[0]).Text = filesController.OpenedFile.FileName;
			}
		}

		private void CloseFile_Click(object sender, RoutedEventArgs e)
		{
			if (TabControlFiles.Items.Count == 0) return;

			DependencyObject dep = (DependencyObject)e.OriginalSource;
			// Traverse the visual tree looking for TabItem
			while ((dep != null) && !(dep is TabItem))
				dep = VisualTreeHelper.GetParent(dep);

			if (dep == null)
			{
				// Didn't find TabItem
				return;
			}

			TabItem? ti = dep as TabItem;

			TabControlFiles.SelectedItem = ti;

			AskToSave();

			filesController.CloseFile();

			TabControlFiles.Items.Remove(ti);
		}

		private void ExitFile_Click(object sender, RoutedEventArgs e)
		{
			System.Windows.Application.Current.Shutdown();
		}

		private void TabControlFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (TabControlFiles.SelectedIndex < 0) return;

			filesController.OpenedFile = filesController.Files[TabControlFiles.SelectedIndex];

			MoveCursorToEndInputTextBox();
		}

		private void CreateNewTab(string tabHeader, string tabContent)
		{
			TabItem tabItem = new TabItem();

			// header
			StackPanel stackPanel = new StackPanel();
			stackPanel.Orientation = Orientation.Horizontal;

			TextBlock header = new TextBlock();
			header.Text = tabHeader;
			header.Margin = new Thickness(0, 0, 5, 0);

			Button close = new Button();
			close.Content = "X";
			close.Background = new SolidColorBrush(Colors.Transparent);
			close.BorderBrush = new SolidColorBrush(Colors.Transparent);
			close.FontSize = 10;
			close.Padding = new Thickness(5, 0, 5, 0);
			close.Click += CloseFile_Click;

			stackPanel.Children.Add(header);
			stackPanel.Children.Add(close);

			// content
			TextBox textBox = new TextBox();
			textBox.Text = tabContent;
			textBox.FontSize = (double)ComboBoxFontSizes.SelectedItem * 96 / 72;
			textBox.TextChanged += TextBoxInput_TextChanged;

			tabItem.Header = stackPanel;
			tabItem.Content = textBox;

			tabItem.AllowDrop = true;

			TabControlFiles.Items.Add(tabItem);
		}

		private void TextBoxInput_TextChanged(object sender, TextChangedEventArgs e)
		{
			string currentText = ((TextBox)sender).Text;
			TextHelper.Text = currentText;
			filesController.OpenedFileTextChanged(currentText);
		}

		private void Undo_Click(object sender, RoutedEventArgs e)
		{
			if (TabControlFiles.Items.Count == 0) return;

			TabItem currentItem = (TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex];

			filesController.OpenedFileTextUndo();

			((TextBox)currentItem.Content).Text = filesController.OpenedFile.Content;

			MoveCursorToEndInputTextBox();
		}

		private void Redo_Click(object sender, RoutedEventArgs e)
		{
			if (TabControlFiles.Items.Count == 0) return;

			TabItem currentItem = (TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex];

			filesController.OpenedFileTextRedo();

			((TextBox)currentItem.Content).Text = filesController.OpenedFile.Content;

			MoveCursorToEndInputTextBox();
		}

		private void Cut_Click(object sender, RoutedEventArgs e)
		{
			if (TabControlFiles.Items.Count == 0) return;
			if (((TextBox)((TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex]).Content).SelectedText != "")
				// Cut the selected text in the control and paste it into the Clipboard.
				((TextBox)((TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex]).Content).Cut();

			FocusOnInputTextBox();
		}

		private void Copy_Click(object sender, RoutedEventArgs e)
		{
			if (TabControlFiles.Items.Count == 0) return;
			if (((TextBox)((TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex]).Content).SelectedText != "")
				// Cut the selected text in the control and paste it into the Clipboard.
				((TextBox)((TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex]).Content).Copy();

			FocusOnInputTextBox();
		}

		private void Paste_Click(object sender, RoutedEventArgs e)
		{
			if (TabControlFiles.Items.Count == 0) return;
			if (Clipboard.GetDataObject().GetDataPresent(DataFormats.Text) == true)
			{
				if (((TextBox)((TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex]).Content).SelectionLength > 0)
				{
					((TextBox)((TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex]).Content).SelectionStart
						= ((TextBox)((TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex]).Content).SelectionStart
						+ ((TextBox)((TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex]).Content).SelectionLength;
				}
				((TextBox)((TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex]).Content).Paste();
			}

			FocusOnInputTextBox();
		}

		private void Delete_Click(object sender, RoutedEventArgs e)
		{
			if (TabControlFiles.Items.Count == 0) return;
			if (((TextBox)((TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex]).Content).SelectionLength > 0)
			{
				((TextBox)((TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex]).Content).SelectedText = "";
			}

			FocusOnInputTextBox();
		}

		private void SelectAll_Click(object sender, RoutedEventArgs e)
		{
			if (TabControlFiles.Items.Count == 0) return;
			((TextBox)((TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex]).Content).SelectAll();

			FocusOnInputTextBox();
		}

		private void FocusOnInputTextBox()
		{
			((TextBox)((TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex]).Content).Focus();
		}

		private void MoveCursorToEndInputTextBox()
		{
			FocusOnInputTextBox();

			((TextBox)((TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex]).Content).SelectionStart =
			((TextBox)((TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex]).Content).Text.Length;
			((TextBox)((TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex]).Content).SelectionLength = 0;
		}

		private void Help_Click(object sender, RoutedEventArgs e)
		{
			Help help = new Help();
			help.ShowDialog();
		}

		private void About_Click(object sender, RoutedEventArgs e)
		{
			AboutProgram aboutProgram = new AboutProgram();
			aboutProgram.ShowDialog();
		}

		private void Poliz_Click(object sender, RoutedEventArgs e)
		{
			if (TabControlFiles.Items.Count == 0) return;

			PolizItem polizItem = new();

			polizItem.PolizString = PolizConverter.InfixToPostfix(filesController.OpenedFile.Content);
			polizItem.Result = PolizSolver.EvaluatePostfix(polizItem.PolizString);

			DataGridOutput.ItemsSource = null;
			DataGridOutput.ItemsSource = new ObservableCollection<PolizItem>() {polizItem};
			TabControlOutput.SelectedIndex = 1;
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{

		}

		private void AskToSave()
		{
			if (filesController.IsContentChanged())
			{
				if (MessageBox.Show("Сохранить изменения в файле?", "Сохранение",
					MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
				{
					filesController.SaveFile();
				}
			}
		}

		private void InitializeComboBoxFontSizes()
		{
			ComboBoxFontSizes.Items.Add(8.0);
			ComboBoxFontSizes.Items.Add(10.0);
			ComboBoxFontSizes.Items.Add(12.0);
			ComboBoxFontSizes.Items.Add(14.0);
			ComboBoxFontSizes.Items.Add(16.0);

			ComboBoxFontSizes.SelectedItem = 12.0;
		}

		private void ComboBoxFontSizes_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			foreach (TabItem ti in TabControlFiles.Items)
			{
				((TextBox)ti.Content).FontSize = (double)ComboBoxFontSizes.SelectedItem * 96 / 72;
			}
		}

		private void Window_Drop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

				// Обрабатывайте каждый перетащенный файл
				foreach (string file in files)
				{
					if (System.IO.File.Exists(file) && System.IO.Path.GetExtension(file).Equals(".txt", StringComparison.OrdinalIgnoreCase))
					{
						if (filesController.Files.Count > 0)
						{
							AskToSave();
						}

						if (filesController.OpenFile(file))
						{
							CreateNewTab(filesController.OpenedFile.FileName, filesController.OpenedFile.Content);
							TabControlFiles.SelectedIndex = TabControlFiles.Items.Count - 1;
						}
					}
				}
			}
		}

		private void Window_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effects = DragDropEffects.Copy;
			}
			else
			{
				e.Effects = DragDropEffects.None;
			}
		}

		private void Start_Click(object sender, RoutedEventArgs e)
		{
			if (TabControlFiles.Items.Count == 0) return;

			TextHelper.Text = filesController.OpenedFile.Content;
			if (lexerController.LexicalAnalysis(filesController.OpenedFile.Content))
			{
				DataGridOutput.ItemsSource = null;
				DataGridOutput.ItemsSource = lexerController.GetLexemesVM();
				TabControlOutput.SelectedIndex = 1;

				TabItem currentItem = (TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex];
				((TextBox)currentItem.Content).Text = lexerController.Code;

				if (!parserController.SyntacticAnalysis(lexerController.GetLexemes()))
				{
					var errors = parserController.GetErrors();
					var correctedLexemes = errorNeutralizerController.NeutralizingErrors(lexerController.GetLexemes());

					if (parserController.SyntacticAnalysis(correctedLexemes))
					{
						var result = MessageBox.Show("В тексте обнаружены ошибки! Исправить?", "Ошибки", MessageBoxButton.YesNo);
						if (result == MessageBoxResult.Yes)
						{
							currentItem = (TabItem)TabControlFiles.Items[TabControlFiles.SelectedIndex];
							((TextBox)currentItem.Content).Text = TextHelper.CreateTextFromLexemes(correctedLexemes);

							lexerController.LexicalAnalysis(filesController.OpenedFile.Content);
							DataGridOutput.ItemsSource = null;
							DataGridErrors.ItemsSource = null;
							DataGridOutput.ItemsSource = lexerController.GetLexemesVM();
							TabControlOutput.SelectedIndex = 1;
						}
						else
						{
							DataGridErrors.ItemsSource = null;
							DataGridErrors.ItemsSource = errors;
							TabControlOutput.SelectedIndex = 0;
						}
					}
					else
					{
						DataGridErrors.ItemsSource = null;
						DataGridErrors.ItemsSource = errors;
						TabControlOutput.SelectedIndex = 0;
					}
				}
			}
			else
			{
				DataGridOutput.ItemsSource = null;
				DataGridErrors.ItemsSource = null;
				DataGridErrors.ItemsSource = lexerController.GetErrors();
				TabControlOutput.SelectedIndex = 0;
			}
		}

		private void FormulationOfTheProblem_Click(object sender, RoutedEventArgs e)
		{
			FormulationOfTheProblem f = new FormulationOfTheProblem();
			f.ShowDialog();
		}

		private void Grammar_Click(object sender, RoutedEventArgs e)
		{
			Grammar grammar = new Grammar();
			grammar.ShowDialog();
		}

		private void GrammarClassification_Click(object sender, RoutedEventArgs e)
		{
			GrammarClassification grammarClassification = new GrammarClassification();
			grammarClassification.ShowDialog();
		}

		private void AnalysisMethod_Click(object sender, RoutedEventArgs e)
		{
			AnalysisMethod analysisMethod = new AnalysisMethod();
			analysisMethod.ShowDialog();
		}

		private void DiagnosticsAndNeutralizationOfErrors_Click(object sender, RoutedEventArgs e)
		{
			DiagnosticsAndNeutralizationOfErrors d = new DiagnosticsAndNeutralizationOfErrors();
			d.ShowDialog();
		}

		private void TextExample_Click(object sender, RoutedEventArgs e)
		{
			TextExample te = new TextExample();
			te.ShowDialog();
		}

		private void Bibliography_Click(object sender, RoutedEventArgs e)
		{
			Bibliography bibliography = new Bibliography();
			bibliography.ShowDialog();
		}

		private void SourceCodes_Click(object sender, RoutedEventArgs e)
		{
			SourceCodes ew = new SourceCodes();
			ew.ShowDialog();
		}
	}
}