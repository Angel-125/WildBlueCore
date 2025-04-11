using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace CraftReplacerGUI
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtDirectory.Text = dialog.SelectedPath;
                    LoadFiles(dialog.SelectedPath);
                }
            }
        }

        private void LoadFiles(string directoryPath, string extension = "craft")
        {
            lstFiles.Items.Clear();
            if (Directory.Exists(directoryPath))
            {
                var files = Directory.GetFiles(directoryPath, "*." + extension, SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (file.Contains("Backup"))
                        continue;

                    lstFiles.Items.Add(GetRelativePath(directoryPath, file));
                }
            }
        }

        private void loadTextReplacements(Dictionary<string, string> replacements)
        {
            replacements.Clear();
            string filePath = AppContext.BaseDirectory + "ModuleReplacements.txt";
            string[] lines = File.ReadAllLines(filePath);

            string[] replacementTexts;
            for (int index = 0; index < lines.Length; index++)
            {
                Console.WriteLine(lines[index]);
                replacementTexts = lines[index].Split(new char[] { ',' });
                if (!replacements.ContainsKey(replacementTexts[0]))
                    replacements.Add(replacementTexts[0], replacementTexts[1]);
            }
            Console.WriteLine(replacements.Keys.Count + " found.");
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            string dir = txtDirectory.Text;
            if (!Directory.Exists(dir))
            {
                MessageBox.Show("Please select a valid directory.");
                return;
            }

            int modifiedCount = makeReplacements(dir);
            makeReplacements(dir, "loadmeta");

            MessageBox.Show("Replacement complete. Files modified: " + modifiedCount);
        }

        private int makeReplacements(string dir, string extension = "craft")
        {
            var replacements = new Dictionary<string, string>();
            loadTextReplacements(replacements);

            int modifiedCount = 0;
            foreach (string filePath in Directory.GetFiles(dir, "*." + extension, SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(filePath);
                bool changed = false;

                foreach (var pair in replacements)
                {
                    if (content.Contains(pair.Key))
                    {
                        content = content.Replace(pair.Key, pair.Value);
                        changed = true;
                    }
                }

                if (changed)
                {
                    string relativePath = GetRelativePath(dir, filePath);
                    string backupRoot = Path.Combine(dir, "Backup");
                    string backupFilePath = Path.Combine(backupRoot, Path.ChangeExtension(relativePath, extension));

                    string backupDir = Path.GetDirectoryName(backupFilePath);
                    if (backupDir != null)
                    {
                        Directory.CreateDirectory(backupDir);
                    }

                    File.Copy(filePath, backupFilePath, true);
                    File.WriteAllText(filePath, content);
                    modifiedCount++;
                }
            }
            return modifiedCount;
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            string dir = txtDirectory.Text;
            if (!Directory.Exists(dir))
            {
                MessageBox.Show("Please select a valid directory.");
                return;
            }

            DialogResult confirm = MessageBox.Show(
                "Are you sure you want to restore all files from backup? This will overwrite any current .craft files with their .backup versions.",
                "Confirm Restore",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirm != DialogResult.Yes)
                return;

            string backupRoot = Path.Combine(dir, "Backups");
            if (!Directory.Exists(backupRoot))
            {
                MessageBox.Show("No backups found.");
                return;
            }

            int restoredCount = restoreFiles(backupRoot, dir, "craft");
            restoreFiles(backupRoot, dir, "loadmeta");

            MessageBox.Show("Restore complete. Files restored: " + restoredCount);
            LoadFiles(dir);
        }

        private int restoreFiles(string backupRoot, string dir, string extension)
        {
            int restoredCount = 0;
            foreach (string backupFile in Directory.GetFiles(backupRoot, "*." + extension, SearchOption.AllDirectories))
            {
                string relativePath = GetRelativePath(backupRoot, backupFile);
                string originalPath = Path.Combine(dir, Path.ChangeExtension(relativePath, "." + extension));

                string originalDir = Path.GetDirectoryName(originalPath);
                if (originalDir != null)
                {
                    Directory.CreateDirectory(originalDir);
                }

                File.Copy(backupFile, originalPath, true);
                restoredCount++;
            }

            return restoredCount;
        }

        private static string GetRelativePath(string basePath, string fullPath)
        {
            Uri baseUri = new Uri(AppendDirectorySeparatorChar(basePath));
            Uri fullUri = new Uri(fullPath);
            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);
            return Uri.UnescapeDataString(relativeUri.ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        private static string AppendDirectorySeparatorChar(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                return path + Path.DirectorySeparatorChar;
            return path;
        }
    }
}
