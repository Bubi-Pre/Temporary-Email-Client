using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace ListEmail
{
    public partial class MainForm : Form
    {
        private string userEmail;
        private string emailDomain;
        private Timer checkMailTimer;
        private List<EmailMessage> emailMessages;

        private DataGridView emailDataGridView;
        private Button generateEmailButton;
        private TextBox emailDisplayBox;

        public MainForm()
        {
            InitializeForm();
            SetupCheckMailTimer();
            emailMessages = new List<EmailMessage>();
        }

        private void InitializeForm()
        {
           
            this.Text = "Temporary Email Client";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(700, 700);

           
            generateEmailButton = new Button
            {
                Text = "Generate Email",
                Location = new Point(250, 50),  
                Size = new Size(200, 30)  
            };
            generateEmailButton.Click += async (s, e) => await CreateEmail();

            
            emailDisplayBox = new TextBox
            {
                Location = new Point(120, 100),
                Width = 450,
                ReadOnly = true
            };

          
            emailDataGridView = new DataGridView
            {
                Location = new Point(15, 150), 
                Size = new Size(665, 500),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersVisible = true,
                RowHeadersVisible = false,
                ReadOnly = true,
                AllowUserToAddRows = false
            };

           
            emailDataGridView.Columns.Add("Subject", "Subject");
            emailDataGridView.Columns.Add("Content", "Content");

            emailDataGridView.CellFormatting += EmailDataGridView_CellFormatting;

            
            emailDataGridView.CellClick += async (s, e) => await ShowEmailDetails(e.RowIndex);

          
            Controls.Add(generateEmailButton);
            Controls.Add(emailDisplayBox);
            Controls.Add(emailDataGridView);
        }

        private void SetupCheckMailTimer()
        {
            checkMailTimer = new Timer { Interval = 10000 };
            checkMailTimer.Tick += async (s, e) => await CheckForNewEmails();
        }

        private async Task CreateEmail()
        {
            try
            {
                string email = await GenerateEmail();
                string[] parts = email.Split('@');
                userEmail = parts[0];
                emailDomain = parts[1];

                emailDisplayBox.Text = email;
                emailDataGridView.Rows.Clear();
                emailMessages.Clear();
                checkMailTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to generate email: " + ex.Message);
            }
        }

        private static async Task<string> GenerateEmail()
        {
            using (var client = new HttpClient())
            {
                string response = await client.GetStringAsync("https://www.1secmail.com/api/v1/?action=genRandomMailbox&count=1");
                var emailList = JsonConvert.DeserializeObject<List<string>>(response);
                return emailList[0];
            }
        }

        private async Task CheckForNewEmails()
        {
            try
            {
                List<EmailMessage> messages = await FetchMessages();
                UpdateEmailList(messages);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error fetching emails: " + ex.Message);
            }
        }

        private async Task<List<EmailMessage>> FetchMessages()
        {
            using (var client = new HttpClient())
            {
                string response = await client.GetStringAsync($"https://www.1secmail.com/api/v1/?action=getMessages&login={userEmail}&domain={emailDomain}");
                return JsonConvert.DeserializeObject<List<EmailMessage>>(response) ?? new List<EmailMessage>();
            }
        }

        private void UpdateEmailList(List<EmailMessage> newMessages)
        {
            foreach (var message in newMessages)
            {
                if (!emailMessages.Exists(m => m.Id == message.Id))
                {
                    emailMessages.Add(message);
                    emailDataGridView.Rows.Add(message.Subject, ""); 
                }
            }
        }

        private async Task ShowEmailDetails(int rowIndex)
        {
            if (rowIndex == -1 || rowIndex >= emailMessages.Count) return;

            var selectedMessage = emailMessages[rowIndex];
            try
            {
                var fullMessage = await FetchFullMessage(selectedMessage.Id);

               
                string plainText = Regex.Replace(fullMessage.HtmlBody ?? fullMessage.TextBody, "<.*?>", string.Empty);

                emailDataGridView.Rows[rowIndex].Cells["Content"].Value = plainText;

               
                emailDataGridView.CellDoubleClick += (s, e) =>
                {
                    if (e.ColumnIndex == 1 && e.RowIndex == rowIndex) 
                    {
                        Clipboard.SetText(plainText);
                        MessageBox.Show("Content copied to clipboard!");
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load message: " + ex.Message);
            }
        }

        private async Task<FullEmailMessage> FetchFullMessage(int messageId)
        {
            using (var client = new HttpClient())
            {
                string url = $"https://www.1secmail.com/api/v1/?action=readMessage&login={userEmail}&domain={emailDomain}&id={messageId}";
                string response = await client.GetStringAsync(url);
                return JsonConvert.DeserializeObject<FullEmailMessage>(response);
            }
        }

        private void EmailDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == 0) 
            {
                e.CellStyle.Font = new Font(DataGridView.DefaultFont, FontStyle.Bold);
            }
            else if (e.ColumnIndex == 1) 
            {
                e.CellStyle.Font = new Font(DataGridView.DefaultFont, FontStyle.Regular);
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
          
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "MainForm";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);

        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
    }

    public class EmailMessage
    {
        public int Id { get; set; }
        public string From { get; set; }
        public string Subject { get; set; }
        public string Date { get; set; }
    }

    public class FullEmailMessage : EmailMessage
    {
        public string Body { get; set; }
        public string TextBody { get; set; }
        public string HtmlBody { get; set; }
    }
}
