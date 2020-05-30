using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Threading;

namespace KeywordSearchingApp
{
    public partial class Form1 : Form
    {
        BinarySearchTree bst;
        bool browsePaths = false;
        string selectedFolder = "";

        public Form1()
        {
            InitializeComponent();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("It's developed by ASU", "About");
        }

        private void txtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedFolder))
            {
                if (e.KeyChar == 13)
                {
                    browsePaths = false;
                    gvPaths.DataSource = null;
                    txtFileContent.Text = "";

                    Node node = bst.Search(txtSearch.Text);
                    if (node != null)
                    {
                        gvPaths.DataSource = node.Paths;
                        browsePaths = true;
                    }
                    else
                    {
                        MessageBox.Show("Not found !!");
                    }
                }
            }
            else
            {
                MessageBox.Show("plz select folder for indexing its content");
            }
        }

        private void gvPaths_SelectionChanged(object sender, EventArgs e)
        {
            if (browsePaths)
            {
                StringBuilder sb = new StringBuilder();
                FileStream fs = new FileStream(gvPaths.SelectedRows[0].Cells[0].Value.ToString(), FileMode.Open, FileAccess.Read);
                StreamReader sr = new StreamReader(fs);
                string line = "";
                while ((line = sr.ReadLine()) != null)
                {
                    sb.AppendLine(line);
                }
                sr.Close();
                fs.Dispose();
                txtFileContent.Text = sb.ToString();
                FoundPath fp = gvPaths.SelectedRows[0].DataBoundItem as FoundPath;

                
                foreach (int index in fp.Indexes)
                {
                    txtFileContent.Select(index == 0 ? index + 1 : index, txtSearch.Text.Length);
                    txtFileContent.SelectionBackColor = Color.Orange;
                }

            }
        }

        private void indexFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                selectedFolder = fbd.SelectedPath;
                lblPath.Text = selectedFolder;

                if (File.Exists("settings.dat"))
                {
                    FileStream fs = new FileStream("settings.dat", FileMode.Open, FileAccess.Read);
                    StreamReader sr = new StreamReader(fs);
                    string setting = sr.ReadLine();
                    sr.Close();
                    fs.Dispose();

                    string[] tokens = setting.Split('|');
                    if (tokens.Length == 2)
                    {
                        if (tokens[0] == fbd.SelectedPath)
                        {
                            DateTime folderLastModifiedDate = Directory.GetLastWriteTime(selectedFolder);
                            folderLastModifiedDate = new DateTime(folderLastModifiedDate.Year, folderLastModifiedDate.Month, folderLastModifiedDate.Day, folderLastModifiedDate.Hour, folderLastModifiedDate.Minute, folderLastModifiedDate.Second);
                            DateTime lastRegisteredModifiedDate = Convert.ToDateTime(tokens[1]);

                            if (folderLastModifiedDate != lastRegisteredModifiedDate)
                            {
                                FileStream fs2 = new FileStream("settings.dat", FileMode.Create, FileAccess.Write);
                                StreamWriter sw = new StreamWriter(fs2);
                                sw.WriteLine(selectedFolder + "|" + Directory.GetLastWriteTime(fbd.SelectedPath).ToString());
                                sw.Close();
                                fs2.Dispose();
                                Thread thread = new Thread(new ThreadStart(BuildIndexing));
                                thread.Start();
                            }
                            else
                            {
                                MessageBox.Show("this folder is already indexed before !!");
                            }
                        }
                        else
                        {
                            FileStream fs2 = new FileStream("settings.dat", FileMode.Create, FileAccess.Write);
                            StreamWriter sw = new StreamWriter(fs2);
                            sw.WriteLine(selectedFolder + "|" + Directory.GetLastWriteTime(fbd.SelectedPath).ToString());
                            sw.Close();
                            fs2.Dispose();
                            Thread thread = new Thread(new ThreadStart(BuildIndexing));
                            thread.Start();
                        }
                    }

                }
                else
                {
                    FileStream fs = new FileStream("settings.dat", FileMode.Create, FileAccess.Write);
                    StreamWriter sw = new StreamWriter(fs);
                    sw.WriteLine(selectedFolder + "|" + Directory.GetLastWriteTime(fbd.SelectedPath).ToString());
                    sw.Close();
                    fs.Dispose();
                    Thread thread = new Thread(new ThreadStart(BuildIndexing));
                    thread.Start();
                }
            }
        }

        private void BuildIndexing()
        {
            bst = new BinarySearchTree();
            progressBar1.Value = 0;

            DirectoryInfo directory = new DirectoryInfo(selectedFolder);
            FileInfo[] files = directory.GetFiles("*.txt");
            //progressBar1.Maximum = files.Length;
            int totalLength = files.Length;
            for (int i = 0; i < files.Length; i++)
            {
                FileInfo file = files[i];
                FileStream fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
                StreamReader sr = new StreamReader(fs);
                string line = "";
                int current_position = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] words = Regex.Split(line, "\\s+");
                    foreach (string word in words)
                    {
                        bst.Insert(word, file.FullName, line.IndexOf(word) + current_position);
                    }
                    current_position += line.Length + 1;
                }
                sr.Close();
                fs.Dispose();
                this.Invoke(new Action(() =>
                {
                    progressBar1.Maximum = files.Length;
                    progressBar1.Value++;
                    lblStatus.Text = (i + 1) + " of " + totalLength;
                }));

            }
            SaveIndexing(bst.Root);
            MessageBox.Show("Indexing has been built successfully and saved !!", "Info");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists("settings.dat"))
            {
                FileStream fs = new FileStream("settings.dat", FileMode.Open, FileAccess.Read);
                StreamReader sr = new StreamReader(fs);
                string setting = sr.ReadLine();
                sr.Close();
                fs.Dispose();

                if (setting != null)
                {
                    string[] tokens = setting.Split('|');
                    if (tokens.Length == 2)
                    {
                        selectedFolder = tokens[0];
                        lblPath.Text = selectedFolder;
                        DateTime folderLastModifiedDate = Directory.GetLastWriteTime(selectedFolder);
                        folderLastModifiedDate = new DateTime(folderLastModifiedDate.Year, folderLastModifiedDate.Month, folderLastModifiedDate.Day, folderLastModifiedDate.Hour, folderLastModifiedDate.Minute, folderLastModifiedDate.Second);
                        DateTime lastRegisteredModifiedDate = Convert.ToDateTime(tokens[1]);
                        if (folderLastModifiedDate != lastRegisteredModifiedDate)
                        {
                            FileStream fs2 = new FileStream("settings.dat", FileMode.Create, FileAccess.Write);
                            StreamWriter sw = new StreamWriter(fs2);
                            sw.WriteLine(selectedFolder + "|" + Directory.GetLastWriteTime(selectedFolder).ToString());
                            sw.Close();
                            fs2.Dispose();
                            Thread thread = new Thread(new ThreadStart(BuildIndexing));
                            thread.Start();
                        }
                        else
                        {
                            LoadBST();
                        }
                    }
                    else
                    {
                        MessageBox.Show("plz identify indexing folder");
                        selectedFolder = "";
                    }
                }
                else
                {
                    MessageBox.Show("plz identify indexing folder");
                    selectedFolder = "";
                }

            }
            else
            {
                MessageBox.Show("plz identify indexing folder");
                selectedFolder = "";
            }
        }

        private void SaveIndexing(Node root)
        {
            StringBuilder sb = new StringBuilder();
            bst.PreOrderTraverse(root, sb);

            FileStream fs = new FileStream("Index.dat", FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            sw.Write(sb.ToString());
            sw.Close();
            fs.Dispose();

        }

        private void LoadBST()
        {
            bst = new BinarySearchTree();
            FileStream fs = new FileStream("index.dat", FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs);
            string line = "";
            while ((line = sr.ReadLine()) != null)
            {
                string[] tokens = line.Split(new string[] { "%20" }, StringSplitOptions.None);
                string keyword = tokens[0];
                string[] fileNamesAndIndexes = tokens[1].Split('|');
                for (int i = 0; i < fileNamesAndIndexes.Length; i++)
                {
                    string[] fileNamesAndIndexesSplitted = fileNamesAndIndexes[i].Split('>');
                    string filename = fileNamesAndIndexesSplitted[0];
                    string[] indexes = fileNamesAndIndexesSplitted[1].Split('*');
                    foreach (string index in indexes)
                    {
                        bst.Insert(keyword, filename, int.Parse(index));
                    }
                }
            }
            sr.Close();
            fs.Dispose();
        }
    }

    class BinarySearchTree : IBinarySearchTree
    {
        private Node root;

        public BinarySearchTree()
        {
            root = null;
        }

        public Node Root
        {
            get
            {
                return root;
            }
        }

        public void Insert(string keyword, string path, int index)
        {
            if (root == null)
            {
                root = new Node(keyword);
                FoundPath newFoundPath = new FoundPath(path);
                newFoundPath.Indexes.Add(index);
                root.Paths.Add(newFoundPath);
            }
            else
            {
                Node ptr = root;
                while (true)
                {
                    if (ptr.Key.Equals(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        bool path_found = false;
                        foreach (FoundPath fp in ptr.Paths)
                        {
                            if (fp.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                            {
                                fp.Indexes.Add(index);
                                path_found = true;
                                break;
                            }
                        }
                        if (!path_found)
                        {
                            FoundPath newFoundPath = new FoundPath(path);
                            newFoundPath.Indexes.Add(index);
                            ptr.Paths.Add(newFoundPath);

                        }
                        break;
                    }
                    else if (keyword.CompareTo(ptr.Key) < 0)
                    {
                        if (ptr.LeftNode != null)
                        {
                            ptr = ptr.LeftNode;
                        }
                        else
                        {
                            ptr.LeftNode = new Node(keyword);

                            FoundPath newFoundPath = new FoundPath(path);
                            newFoundPath.Indexes.Add(index);
                            ptr.LeftNode.Paths.Add(newFoundPath);
                            break;
                        }
                    }
                    else
                    {
                        if (ptr.RightNode != null)
                        {
                            ptr = ptr.RightNode;
                        }
                        else
                        {
                            ptr.RightNode = new Node(keyword);

                            FoundPath newFoundPath = new FoundPath(path);
                            newFoundPath.Indexes.Add(index);
                            ptr.RightNode.Paths.Add(newFoundPath);
                            break;
                        }
                    }
                }
            }
        }

        public Node Search(string keyword)
        {
            Node result = null;
            Node ptr = root;
            while (ptr != null)
            {
                if (ptr.Key.Equals(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    result = ptr;
                    break;
                }
                else if (keyword.CompareTo(ptr.Key) < 0)
                {
                    ptr = ptr.LeftNode;
                }
                else
                {
                    ptr = ptr.RightNode;
                }
            }
            return result;
        }

        public void PreOrderTraverse(Node _root, StringBuilder sb)
        {
            if (_root != null)
            {
                sb.Append(_root.Key + "%20");
                for (int i = 0; i < _root.Paths.Count; i++)
                {
                    FoundPath path = _root.Paths[i];
                    sb.Append(path.Path + ">");
                    for (int j = 0; j < path.Indexes.Count; j++)
                    {
                        sb.Append(path.Indexes[j]);
                        if ((j + 1) != path.Indexes.Count)
                            sb.Append("*");
                    }
                    if ((i + 1) != _root.Paths.Count)
                        sb.Append("|");
                }
                sb.AppendLine();
                PreOrderTraverse(_root.LeftNode, sb);
                PreOrderTraverse(_root.RightNode, sb);
            }
        }
    }

    interface IBinarySearchTree
    {
        void Insert(string keyword, string path, int index);
        Node Search(string keyword);
        void PreOrderTraverse(Node root, StringBuilder sb);
    }

    class Node
    {
        public Node(string key)
        {
            Key = key;
            Paths = new List<FoundPath>();
        }
        public string Key { set; get; }
        public List<FoundPath> Paths { set; get; }

        public Node LeftNode { set; get; }
        public Node RightNode { set; get; }
    }

    class FoundPath
    {
        public FoundPath(string path)
        {
            Path = path;
            Indexes = new List<int>();
        }
        public string Path { set; get; }
        public List<int> Indexes { set; get; }
    }
}
