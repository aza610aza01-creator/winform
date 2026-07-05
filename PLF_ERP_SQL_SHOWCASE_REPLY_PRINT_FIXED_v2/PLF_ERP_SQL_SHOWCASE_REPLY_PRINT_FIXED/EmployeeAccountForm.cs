using System;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace PremiumLivingFurnitureWinForms;

public class EmployeeAccountForm : Form
{
    readonly TextBox username = new();
    readonly TextBox fullName = new();
    readonly TextBox email = new();
    readonly ComboBox role = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    readonly ComboBox status = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    readonly Label note = new();

    public Dictionary<string, object?> Values { get; } = new();

    public EmployeeAccountForm()
    {
        Text = "Add Employee Account";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(850, 520);
        Font = Theme.DefaultFont;
        BackColor = Theme.AppBg;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(26),
            AutoScroll = true,
            BackColor = Theme.AppBg
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(root, "Username *", username);
        AddRow(root, "Role *", role);
        AddRow(root, "Full Name *", fullName);
        AddRow(root, "Email *", email);
        AddRow(root, "Status *", status);

        note.Text = "A one-time temporary password will be generated automatically and emailed to the new employee. The administrator will not see the password.";
        note.ForeColor = Theme.Primary;
        note.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        note.Dock = DockStyle.Fill;
        note.Padding = new Padding(0, 8, 0, 8);

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        root.SetColumnSpan(note, 2);
        root.Controls.Add(note, 0, root.RowCount++);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Theme.AppBg
        };

        var save = Theme.PrimaryButton("Create Account", 145);
        var cancel = Theme.SecondaryButton("Cancel", 110);

        save.Click += (_, _) => Save();
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.SetColumnSpan(buttons, 2);
        root.Controls.Add(buttons, 0, root.RowCount++);

        Controls.Add(root);

        LoadLookups();
    }

    static void AddRow(TableLayoutPanel root, string label, Control input)
    {
        int row = root.RowCount++;

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

        root.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.Muted,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        }, 0, row);

        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(0, 8, 0, 8);

        root.Controls.Add(input, 1, row);
    }

    void LoadLookups()
    {
        foreach (var item in Database.GetLookup("UserRoles"))
            role.Items.Add(item);

        if (role.Items.Count > 0)
            role.SelectedIndex = 0;

        status.Items.AddRange(new object[] { "Active", "Inactive" });
        status.SelectedIndex = 0;
    }

    void Save()
    {
        string u = username.Text.Trim();
        string fn = fullName.Text.Trim();
        string mail = email.Text.Trim();

        if (string.IsNullOrWhiteSpace(u))
        {
            MessageBox.Show("Please enter username.");
            return;
        }

        if (role.SelectedItem is not LookupItem selectedRole)
        {
            MessageBox.Show("Please select a role.");
            return;
        }

        if (string.IsNullOrWhiteSpace(fn))
        {
            MessageBox.Show("Please enter full name.");
            return;
        }

        if (string.IsNullOrWhiteSpace(mail) || !mail.Contains("@"))
        {
            MessageBox.Show("Please enter a valid email address.");
            return;
        }

        // Important:
        // Check duplicate username BEFORE sending the temporary password email.
        // This prevents sending an email when the account cannot be created.
        try
        {
            if (UsernameExists(u))
            {
                MessageBox.Show(
                    "This username already exists. Please use another username. No email was sent and no account was created.",
                    "Duplicate Username",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Unable to check whether the username already exists.\n\n" + ex.Message,
                "Username Check Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            return;
        }

        string temporaryPassword = GenerateTemporaryPassword();

        try
        {
            EmailService.SendTemporaryPassword(mail, u, temporaryPassword);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Account was not created because the one-time password email could not be sent.\n\n" + ex.Message,
                "Email Failed"
            );
            return;
        }

        Values.Clear();
        Values["Username"] = u;
        Values["Password"] = PasswordService.HashPassword(temporaryPassword);
        Values["RoleId"] = selectedRole.Id;
        Values["FullName"] = fn;
        Values["Email"] = mail;
        Values["Status"] = status.Text;

        MessageBox.Show(
            "Employee account created. The one-time temporary password has been emailed to the employee.",
            "Account Created"
        );

        DialogResult = DialogResult.OK;
    }

    static bool UsernameExists(string username)
    {
        using var c = Database.GetConnection();
        c.Open();

        using var cmd = new MySqlCommand(
            "SELECT COUNT(*) FROM Users WHERE Username = @username",
            c
        );

        cmd.Parameters.AddWithValue("@username", username.Trim());

        object? result = cmd.ExecuteScalar();

        return Convert.ToInt32(result) > 0;
    }

    static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@$%";

        Span<byte> bytes = stackalloc byte[12];
        RandomNumberGenerator.Fill(bytes);

        char[] result = new char[12];

        for (int i = 0; i < result.Length; i++)
            result[i] = chars[bytes[i] % chars.Length];

        return new string(result);
    }
}