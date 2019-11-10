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
                TextContainer.Clear();
                CheckBinaryFileFormat(FileDialog.FileName);
            }
        }
        private void OpenFolderMenu_Click(object sender, EventArgs e)
        {
            if (FolderDialog.ShowDialog() == DialogResult.OK)
            {
                Sidebar.Nodes.Clear();
                TextContainer.Clear();

                string[] paths = Directory.EnumerateFiles(FolderDialog.SelectedPath, "*.bin", SearchOption.AllDirectories).ToArray();

                foreach (string path in paths)
                {
                    CheckBinaryFileFormat(path);
                }
            }
        }
        private void CheckBinaryFileFormat(string path)
        {
            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                if (reader.ReadInt32() != 1 || reader.ReadInt32() != 1)
                {
                    TextContainer.Enabled = true;
                    Sidebar.Nodes.Add(ConvertMapBinaryToNode(path));
                    return;
                }

                reader.SetPosition(0);

                if (reader.ReadInt32() == 1 && reader.ReadInt32() == 1)
                {
                    TextContainer.Enabled = true;
                    Sidebar.Nodes.Add(ConvertSupportBinaryToNode(path));
                    return;
                }
            }
        }
        private bool nodeIsMap(TreeNode node)
        {
            if (node.ToolTipText == "map")
            {
                return true;
            } else
            {
                return false;
            }
        }
        private TreeNode ConvertSupportBinaryToNode(string path)
        {
            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                TreeNode node = new TreeNode
                {
                    Text = Path.GetFileName(path),
                    Name = path,
                    ContextMenuStrip = MainContextMenu,
                    ToolTipText = "support"
                };

                reader.SetPosition(0x10);
                int entryCount = reader.ReadInt32();

                if (entryCount == 0)
                {
                    return new TreeNode();
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

                return node;
            }
        }
        private TreeNode ConvertMapBinaryToNode(string path)
        {
            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                TreeNode node = new TreeNode
                {
                    Text = Path.GetFileName(path),
                    Name = path,
                    ContextMenuStrip = MainContextMenu,
                    ToolTipText = "map"
                };

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

                return node;
            }
        }
        private byte[] ConvertNodeToMapBinary(TreeNode node)
        {
            using(MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    int startTextOffset = (node.Nodes.Count * 8) + 4;
                    int offset = startTextOffset;

                    writer.WriteInt32(node.Nodes.Count);

                    foreach (TreeNode rawNode in node.Nodes)
                    {
                        string text = rawNode.Text.Replace("\r\n", "\n");

                        UTF8Encoding enc = new UTF8Encoding(false);

                        int textLenBytes = enc.GetByteCount(text);

                        writer.WriteInt32(offset);
                        writer.WriteInt32(textLenBytes);

                        long tempOfffset = writer.GetPosition();
                        writer.SetPosition(offset);
                        writer.WriteString(text, Encoding.UTF8);
                        writer.SetPosition(tempOfffset);
                        offset += textLenBytes+1;
                    }
                }

                return stream.ToArray();
            }
        }
        private byte[] ConvertNodeToSupportBinary(TreeNode node)
        {
            using(MemoryStream stream = new MemoryStream())
            {
                using(BinaryWriter writer = new BinaryWriter(stream))
                {
                    List<string> text = new List<string>();
                    foreach(TreeNode rawNode in node.Nodes)
                    {
                        text.Add(rawNode.Text);
                    }

                    writer.WriteInt32(1);
                    writer.WriteInt32(1);
                    writer.WriteInt32(0x20);
                    writer.SetPosition(0x10);
                    writer.WriteInt32(text.Count);
                    writer.SetPosition(0x20);

                    int ofs = 0;
                    for (int i = 0; i < text.Count; i++)
                    {
                        writer.WriteInt32(ofs);
                        ofs += text[i].GetStringLengthWithStopByte(Encoding.UTF8);
                    }
                    writer.WriteInt32(ofs);

                    writer.Seek(0x10);
                    long curr = writer.GetPosition();
                    writer.SetPosition(0x0C);
                    writer.WriteInt32((int)curr - 0x20);
                    writer.SetPosition(curr);

                    for (int i = 0; i < text.Count; i++)
                    {
                        writer.WriteStringStopByte(text[i], Encoding.UTF8);
                    }
                }

                return stream.ToArray();
            }
        }
        private void Sidebar_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (Sidebar.SelectedNode.Name == "raw")
                TextContainer.Text = Sidebar.SelectedNode.Text;
        }
        private void TextContainer_TextChanged(object sender, EventArgs e)
        {
            if (TextContainer.Text != "")
                Sidebar.SelectedNode.Text = TextContainer.Text;
        }
        private void SaveMenu_Click(object sender, EventArgs e)
        {
            SaveDialog.FileName = Sidebar.SelectedNode.Text;

            if (SaveDialog.ShowDialog() == DialogResult.OK)
            {
                if (nodeIsMap(Sidebar.SelectedNode))
                {
                    byte[] bytes = ConvertNodeToMapBinary(Sidebar.SelectedNode);

                    File.WriteAllBytes(SaveDialog.FileName, bytes);
                } else
                {
                    byte[] bytes = ConvertNodeToSupportBinary(Sidebar.SelectedNode);

                    File.WriteAllBytes(SaveDialog.FileName, bytes);
                }
            }
        }
    }
}
