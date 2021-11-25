using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.Net;

namespace HaloInfiniteServerBlock
{
    public partial class Main : Form
    {
        private string HostsFile = "C:\\Windows\\System32\\drivers\\etc\\hosts";
        private string LocalServerFile = "servers.json";
        private List<Server> ServerList;

        public Main()
        {
            InitializeComponent();

            Shown += (s, e) =>
            {
                if (!File.Exists(LocalServerFile))
                {
                    MessageBox.Show("Failed to locate the server list file\n\nCheck https://github.com/CodeSk3tch/HaloInfinite-Server-Block updates and help\n\nApplication will now exit", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                }
                else
                {
                    ReadServerListFile(ref ServerList);
                    UpdateServerList(ref ServerList);
                    SetServerListToUI(ref ServerList);
                }
            };

            ApplyButton.Click += (s, e) =>
            {
                GetServerListFromUI(ref ServerList);
                UpdateHostsFile(ref ServerList);
                UpdateServerList(ref ServerList);
                SetServerListToUI(ref ServerList);
                MessageBox.Show("Hosts file has been updated", "Changes applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            RefreshButton.Click += (s, e) =>
            {
                ReadServerListFile(ref ServerList);
                UpdateServerList(ref ServerList);
                SetServerListToUI(ref ServerList);
            };
        }

        private void SetServerListToUI(ref List<Server> servers)
        {
            listView1.BeginUpdate();
            listView1.Items.Clear();
            listView1.Groups.Clear();
            ListViewItem item;
            ListViewGroup group;
            foreach (var server in servers)
            {
                item = new ListViewItem("");
                item.SubItems.Add(server.InfiniteServer.Region);
                item.SubItems.Add(server.InfiniteServer.Location);
                item.Checked = server.IsBlocked;

                group = listView1.Groups[server.InfiniteServer.Geography];
                if (group == null)
                {
                    group = new ListViewGroup(server.InfiniteServer.Geography, server.InfiniteServer.Geography);
                    listView1.Groups.Add(group);
                    item.Group = group;
                }
                item.Group = group;
                listView1.Items.Add(item);
            }
            listView1.EndUpdate();
        }

        private void GetServerListFromUI(ref List<Server> servers)
        {
            if (listView1.Items.Count == 0) return;
            foreach (var server in servers)
            {
                foreach (ListViewItem item in listView1.Items)
                {
                    if (item.SubItems[1].Text == server.InfiniteServer.Region)
                    {
                        server.IsBlocked = item.Checked;
                    }
                }
            }
        }

        private void DisableReadOnlyHostsFile()
        {
            FileInfo file = new FileInfo(HostsFile);
            if (file.Exists && file.IsReadOnly)
            {
                File.SetAttributes(HostsFile, (File.GetAttributes(HostsFile) & ~FileAttributes.ReadOnly));
            }
        }

        private bool ReadServerListFile(ref List<Server> servers)
        {
            FileInfo file = new FileInfo(LocalServerFile);
            if (!file.Exists) return false;
            servers = new List<Server>();
            string buffer;
            using (var stream = file.OpenText())
            {
                buffer = stream.ReadToEnd();
                stream.Close();
            }
            var temp = new JavaScriptSerializer().Deserialize<List<InfiniteServer>>(buffer);
            foreach (var t in temp)
            {
                servers.Add(new Server() { InfiniteServer = t, IsBlocked = false });
            }
            return true;
        }

        private void UpdateServerList(ref List<Server> servers)
        {
            List<string> buffer = new List<string>();
            using (var stream = new StreamReader(HostsFile, Encoding.UTF8))
            {
                buffer.AddRange(stream.ReadToEnd().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries));
                stream.Close();
            }
            foreach (var server in servers)
            {
                server.IsBlocked = buffer.ContainsAll(server.HostEntries);
            }
        }

        private void UpdateHostsFile(ref List<Server> servers)
        {
            DisableReadOnlyHostsFile();
            List<string> buffer = new List<string>();
            using (var stream = new StreamReader(HostsFile, Encoding.UTF8))
            {
                buffer.AddRange(stream.ReadToEnd().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries));
                stream.Close();
            }
            foreach (var server in servers)
            {
                if (server.IsBlocked)
                {
                    if (!buffer.ContainsAll(server.HostEntries))
                    {
                        foreach (var entry in server.HostEntries)
                        {
                            if (!buffer.Contains(entry)) buffer.Add(entry);
                        }
                    }
                }
                else
                {
                    if (buffer.ContainsAny(server.HostEntries))
                    {
                        foreach (var entry in server.HostEntries)
                        {
                            if (buffer.Contains(entry)) buffer.RemoveAll(e => e == entry);
                        }
                    }
                }
            }
            using (var stream = new StreamWriter(File.Open(HostsFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite), Encoding.UTF8))
            {
                foreach (var line in buffer)
                {
                    stream.WriteLine(line);
                }
                stream.Flush();
                stream.Close();
            }
        }

        public class Server
        {
            public InfiniteServer InfiniteServer;
            public bool IsBlocked;
            public string[] HostEntries
            {
                get
                {
                    var count = InfiniteServer.Hostnames.Count;
                    if (count == 0) return null;
                    var result = new string[count];
                    for (int i = 0; i < count; i++)
                    {
                        result[i] = $"0.0.0.0 {InfiniteServer.Hostnames[i]}";
                    }
                    return result;
                }
            }
        }

        public class InfiniteServer
        {
            public string Geography;
            public string Region;
            public string Location;
            public List<string> Hostnames;
        }

    }

    public static class Extensions
    {
        public static bool ContainsAll(this List<string> a, params string[] b)
        {
            foreach (var x in b)
            {
                if (!a.Contains(x)) return false;
            }
            return true;
        }

        public static bool ContainsAny(this List<string> a, params string[] b)
        {
            foreach (var x in b)
            {
                if (a.Contains(x)) return true;
            }
            return true;
        }
    }

}