using System;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace PremiumLivingFurnitureWinForms;

public class ProfileForm : Form
{
    readonly string username;
    readonly TextBox txtUsername = new() { ReadOnly = true };
    readonly TextBox txtOldPassword = new() { UseSystemPasswordChar = true };
    readonly TextBox txtNewPassword = new() { UseSystemPasswordChar = true };
    readonly TextBox txtConfirmPassword = new() { UseSystemPasswordChar = true };
    readonly CheckBox showPasswords = new() { Text = "Show passwords", AutoSize = true };
    int userId;
    string storedPassword = "";

    public ProfileForm(string username)
    {
        this.username = username.Trim();
        Text = "Update Profile - Change Password";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(760, 600);
        MinimumSize = new Size(740, 560);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        Font = Theme.DefaultFont;
        BackColor = Theme.AppBg;

        Controls.Add(Build());
        LoadProfile();
    }

    Control Build()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.AppBg,
            Padding = new Padding(28),
            AutoScroll = true
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));

        root.Controls.Add(new Label
        {
            Text = "Update Profile" + Environment.NewLine + "Change your password securely. Personal details are managed by Admin.",
            Dock = DockStyle.Fill,
            ForeColor = Theme.Text,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            BackColor = Theme.AppBg,
            AutoSize = false
        }, 0, 0);

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(26),
            MinimumSize = new Size(0, 330),
            AutoScroll = true
        };

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 0,
            BackColor = Color.White,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(form, "Username", txtUsername);
        AddSection(form, "Change Password");
        AddRow(form, "Old Password", txtOldPassword);
        AddRow(form, "New Password", txtNewPassword);
        AddRow(form, "Confirm Password", txtConfirmPassword);

        showPasswords.CheckedChanged += (_, _) =>
        {
            bool hide = !showPasswords.Checked;
            txtOldPassword.UseSystemPasswordChar = hide;
            txtNewPassword.UseSystemPasswordChar = hide;
            txtConfirmPassword.UseSystemPasswordChar = hide;
        };
        int row = form.RowCount++;
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        form.Controls.Add(new Label { BackColor = Color.White }, 0, row);
        form.Controls.Add(showPasswords, 1, row);

        card.Controls.Add(form);
        root.Controls.Add(card, 0, 1);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Theme.AppBg,
            Padding = new Padding(0, 8, 0, 0)
        };
        var save = Theme.PrimaryButton("Save", 120);
        var cancel = Theme.SecondaryButton("Cancel", 110);
        save.Click += (_, _) => SavePassword();
        cancel.Click += (_, _) => Close();
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 2);

        return root;
    }

    static void AddRow(TableLayoutPanel form, string label, Control input)
    {
        int row = form.RowCount++;
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        form.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.Muted,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            BackColor = Color.White
        }, 0, row);
        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(0, 8, 0, 8);
        input.Font = new Font("Segoe UI", 10);
        form.Controls.Add(input, 1, row);
    }

    static void AddSection(TableLayoutPanel form, string title)
    {
        int row = form.RowCount++;
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        var label = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.Primary,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            BackColor = Color.White
        };
        form.SetColumnSpan(label, 2);
        form.Controls.Add(label, 0, row);
    }

    void LoadProfile()
    {
        using var c = Database.GetConnection();
        c.Open();
        using var cmd = new MySqlCommand("SELECT Id, Username, Password FROM Users WHERE Username=@username AND Status='Active' LIMIT 1", c);
        cmd.Parameters.AddWithValue("@username", username);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            MessageBox.Show("Profile not found or account is inactive.");
            Close();
            return;
        }

        userId = Convert.ToInt32(reader["Id"]);
        storedPassword = Convert.ToString(reader["Password"]) ?? "";
        txtUsername.Text = Convert.ToString(reader["Username"]) ?? "";
    }

    void SavePassword()
    {
        string oldPassword = txtOldPassword.Text;
        string newPassword = txtNewPassword.Text;
        string confirmPassword = txtConfirmPassword.Text;

        if (string.IsNullOrWhiteSpace(oldPassword)) { MessageBox.Show("Please enter your old password."); return; }
        if (!PasswordService.VerifyPassword(oldPassword.Trim(), storedPassword)) { MessageBox.Show("Old password is incorrect."); return; }
        if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword)) { MessageBox.Show("Please enter the new password twice."); return; }
        if (newPassword != confirmPassword) { MessageBox.Show("New password and confirm password do not match."); return; }
        if (newPassword.Length < 6) { MessageBox.Show("New password must be at least 6 characters."); return; }
        if (oldPassword == newPassword) { MessageBox.Show("New password cannot be the same as old password."); return; }

        string passwordToSave = PasswordService.HashPassword(newPassword);
        using var c = Database.GetConnection();
        c.Open();
        using var cmd = new MySqlCommand("UPDATE Users SET Password=@password WHERE Id=@id", c);
        cmd.Parameters.AddWithValue("@password", passwordToSave);
        cmd.Parameters.AddWithValue("@id", userId);
        cmd.ExecuteNonQuery();

        storedPassword = passwordToSave;
        txtOldPassword.Clear();
        txtNewPassword.Clear();
        txtConfirmPassword.Clear();
        MessageBox.Show("Password updated successfully.");
        Close();
    }
}
