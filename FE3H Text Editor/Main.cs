using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IOExtension;

namespace FE3H_Text_Editor
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            Text += $" v{ConfigurationManager.AppSettings["Version"]}";
        }
        private void OpenFileMenu_Click(object sender, EventArgs e)
        {
            FileDialog.Filter = "Binary file (*.bin)|*.bin";
            if (FileDialog.ShowDialog() == DialogResult.OK)
            {
                Sidebar.Nodes.Clear();

                TreeNode Node = new TreeNode(Path.GetFileName(FileDialog.FileName))
                {
                    Name = FileDialog.FileName,
                    ContextMenuStrip = ContextMenu
                };

                Sidebar.Nodes.Add(Node);
                Sidebar.Enabled = true;
            }
        }
        private void OpenFolderMenu_Click(object sender, EventArgs e)
        {
            if (FolderDialog.ShowDialog() == DialogResult.OK)
            {
                Sidebar.Nodes.Clear();

                string[] files = Directory.EnumerateFiles(FolderDialog.SelectedPath, "*.*", SearchOption.AllDirectories).ToArray();

                foreach (var file in files)
                {
                    TreeNode Node = new TreeNode(Path.GetFileName(file))
                    {
                        Name = file,
                        ContextMenuStrip = ContextMenu
                    };

                    Sidebar.Nodes.Add(Node);
                }

                Sidebar.Enabled = true;
            }
        }
        private void Sidebar_AfterSelect(object sender, TreeViewEventArgs e)
        {
            Sidebar.SelectedNode.Nodes.Clear();
            
            if (Sidebar.SelectedNode.Name == "raw")
            {
                TextContainer.Enabled = true;
                TextContainer.Text = Sidebar.SelectedNode.Text;
                return;
            }

            using(var reader = new BinaryReader(File.OpenRead(Sidebar.SelectedNode.Name)))
            {
                if (reader.ReadInt32() != 1 || reader.ReadInt32() != 1)
                {
                    ConvertMapBinaryToText(Sidebar.SelectedNode);
                    return;
                }

                reader.SetPosition(0);

                if (reader.ReadInt32() == 1 && reader.ReadInt32() == 1)
                {
                    ConvertSupportBinaryToText(Sidebar.SelectedNode);
                    return;
                }

                MessageBox.Show("Unsupported file format.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ConvertSupportBinaryToText(TreeNode node)
        {
            using (var reader = new BinaryReader(File.OpenRead(node.Name)))
            {
                reader.SetPosition(0x10);
                int entryCount = reader.ReadInt32();

                if (entryCount == 0)
                {
                    MessageBox.Show("Unsupported file format.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                reader.SetPosition(0x24 + (entryCount * 4));

                reader.Seek(0x10);

                for (int i = 0; i < entryCount; i++)
                {
                    TreeNode textNode = new TreeNode(reader.ReadStringToStopByte(Encoding.UTF8).Replace("\n", "\r\n"))
                    {
                        Name = "raw"
                    };
                    node.Nodes.Add(textNode);
                }
            }
        }
        private void ConvertMapBinaryToText(TreeNode node)
        {
            using (var reader = new BinaryReader(File.OpenRead(node.Name)))
            {
                int entryCount = reader.ReadInt32();

                Dictionary<int, int> entries = new Dictionary<int, int>();

                for (int i = 0; i < entryCount; i++)
                {
                    entries.Add(reader.ReadInt32(), reader.ReadInt32());
                }

                foreach (var entry in entries)
                {
                    reader.SetPosition(entry.Key);
                    TreeNode textNode = new TreeNode(reader.ReadString(Encoding.UTF8, entry.Value).Replace("\n", "\r\n"))
                    {
                        Name = "raw"
                    };
                    node.Nodes.Add(textNode);
                }
            }
        }
    }
}
