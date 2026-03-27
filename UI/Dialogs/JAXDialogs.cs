namespace JAXBase.UI.Dialogs
{
    public class JAXDialogs
    {
        public int AssertDialog(string AssertMsg)
        {
            DialogResult res = DialogResult.OK;
            int buttonWidth = 100;

            using (var dialog = new CustomMessageBox(0, 525, 350, AssertMsg, "Assert Triggered", buttonWidth, ["Debug", "Cancel", "Ignore", "Ignore All"], FormStartPosition.CenterScreen))
            {
                res = dialog.ShowDialog();
            }

            return res switch
            {
                DialogResult.Ignore => 2,
                DialogResult.Cancel => 3,
                DialogResult.Yes => 4,
                DialogResult.No => 5,
                _ => 1
            };
        }

        public string InputBox(string text, string caption)
        {
            string res = string.Empty;
            int buttonWidth = 100;

            using (var dialog = new CustomMessageBox(1, 525, 350, text, caption.Length > 0 ? caption : "Input Requested", buttonWidth, ["Debug", "Cancel", "Ignore", "Ignore All"], FormStartPosition.CenterScreen))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    res = dialog.UserInputText.TrimEnd();
                }
            }

            return res;
        }


        partial class CustomMessageBox : Form
        {
            int BoxType = 0;
            Label Message1 = new();
            TextBox Text1 = new() { Visible = false };
            Button Btn1 = new() { Visible = false };
            Button Btn2 = new() { Visible = false };
            Button Btn3 = new() { Visible = false };
            Button Btn4 = new() { Visible = false };
            Button Btn5 = new() { Visible = false };

            /*
             * 
             * Type
             *    0 Dialog
             *    1 Input Box
             * 
             */
            public CustomMessageBox(int type, int wdth, int hgt, string text, string caption, int buttonWidth, string[] buttonCaptions, FormStartPosition fspos)
            {
                StartPosition = fspos;
                Width = wdth;
                Height = hgt;
                Text = caption;
                BoxType = type;

                int buttonCount = buttonCaptions.Length;

                if (type == 0)
                {
                    Message1 = new() { Left = 25, Top = 25, MaximumSize = new(wdth - 50, hgt - 100), AutoSize = true, Text = text, Visible = true };
                }
                else
                {
                    Message1 = new() { Left = 25, Top = 25, MaximumSize = new(wdth - 50, hgt - 175), AutoSize = true, Text = text, Visible = true };
                    Text1 = new() { Left = 25, Height = Message1.Height, Top = 100, Width = wdth - 50, Visible = true };
                }

                Btn1 = new() { Width = buttonWidth, Top = hgt - 75, Text = buttonCount > 0 ? buttonCaptions[0] : "OK", Visible = true };
                Btn2 = new() { Width = buttonWidth, Top = hgt - 75, Text = buttonCount > 1 ? buttonCaptions[1] : "2", Visible = buttonCount > 1 };
                Btn3 = new() { Width = buttonWidth, Top = hgt - 75, Text = buttonCount > 2 ? buttonCaptions[2] : "3", Visible = buttonCount > 2 };
                Btn4 = new() { Width = buttonWidth, Top = hgt - 75, Text = buttonCount > 3 ? buttonCaptions[3] : "4", Visible = buttonCount > 3 };
                Btn5 = new() { Width = buttonWidth, Top = hgt - 75, Text = buttonCount > 4 ? buttonCaptions[4] : "5", Visible = buttonCount > 4 };

                int BtnSpace = Width - 25 - (buttonWidth + 25) * buttonCount;

                Btn1.Left = BtnSpace / (1 * buttonCount) + 25;
                Btn2.Left = buttonCount > 1 ? Btn1.Left + buttonWidth + 25 : wdth - 5;
                Btn3.Left = buttonCount > 2 ? Btn2.Left + buttonWidth + 25 : wdth - 5;
                Btn4.Left = buttonCount > 3 ? Btn3.Left + buttonWidth + 25 : wdth - 5;
                Btn5.Left = buttonCount > 4 ? Btn4.Left + buttonWidth + 25 : wdth - 5;
            }

            public string UserInputText
            {
                get { return Text1.Text; }
            }

            private void Btn1_Click(object sender, EventArgs e)
            {
                DialogResult = DialogResult.OK;
                Close();
            }

            private void Btn2_Click(object sender, EventArgs e)
            {
                DialogResult = DialogResult.Ignore;
                Close();
            }

            private void Btn3_Click(object sender, EventArgs e)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }

            private void Btn4_Click(object sender, EventArgs e)
            {
                DialogResult = DialogResult.Yes;
                Close();
            }

            private void Btn5_Click(object sender, EventArgs e)
            {
                DialogResult = DialogResult.No;
                Close();
            }

            private void CustomMessageBox_Load(object sender, EventArgs e)
            {
                Name = "JAXDialog";
                Size = new() { Height = 250, Width = 525 };
                Dock = DockStyle.None;
            }

            private void CustomMesssagebox_Resize(object sender, EventArgs e)
            {
                // Resize the Message box correctly
                //string msg = Message1.Text.TrimEnd();
                //Message1.Text = msg;
                Message1.AutoSize = false;
                Message1.MaximumSize = new(Width - 50, Height - BoxType == 0 ? 100 : 175);
                //Message1.Text = msg + " ";
            }
        }
    }
}
