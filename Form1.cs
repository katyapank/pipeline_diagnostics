using Aspose.Words;
using Aspose.Words.Drawing;
using BitMiracle.Docotic.Pdf;
using MaterialSkin.Controls;
using System;

using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Image = System.Drawing.Image;
using MessageBox = System.Windows.Forms.MessageBox;
using Size = System.Drawing.Size;
using ToolTip = System.Windows.Forms.ToolTip;
using MaterialSkin;

namespace WindowsFormsApp2
{

    public partial class Form1 : MaterialForm
    {


        private class FrPoint
        {
            public int npip;
            public int nsector;
            public int npoint;
            public DateTime dcheck;
            public double freq;


            public FrPoint(int npip1, int nsector1, int npoint1, DateTime dcheck1, double freq1)
            {
                npip = npip1;
                nsector = nsector1;
                npoint = npoint1;
                dcheck = dcheck1;
                freq = freq1;
            }
        }

        private class FrDev
        {
            public int npip;
            public int nsection;
            public int npoint;
            public double freq;
            public double avgfreq;
            public double deviation;
            public DateTime dcheck;


            public FrDev(int npip1, int nsection1, int npoint1, double freq1, double avgfreq1, double deviation1, DateTime dcheck1)
            {
                npip = npip1;
                nsection = nsection1;
                npoint = npoint1;
                freq = freq1;
                avgfreq = avgfreq1;
                deviation = deviation1;
                dcheck = dcheck1;
            }
        }


        private readonly MaterialSkinManager materialSkinManager2 = MaterialSkinManager.Instance;
        private bool autoload = true;
        private List<FrPoint> frlist = new List<FrPoint>();
        private List<FrDev> frdevl = new List<FrDev>();
        private List<FrDev> frdevl1 = new List<FrDev>();

        private bool hints = true;
        private int interval = 60000; //1 min

        public Form1()
        {
            InitializeComponent();
            panel1.Controls.Add(chart1);
            panel3.Controls.Add(materialComboBox1);
            panel3.Controls.Add(materialComboBox2);
            panel3.Controls.Add(materialComboBox3);
            panel2.Controls.Add(pictureBox1);

            panel1.Controls.Add(materialButton3);
            panel1.Controls.Add(materialButton4);

            tick_start();

            ToolTip t = new ToolTip();
            t.SetToolTip(label4, "Двойной клик чтобы ввести значение для масштабирования");
        }

        private void tick_start()
        {
            timer1.Interval = interval;
            timer1.Tick += new System.EventHandler(timer1_Tick);
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            chart_Load();
        }

        bool start = false;

        private void Form1_Load(object sender, EventArgs e)
        {

            bd_things();
            chart_Load();
            loadImage();

            start = true;
        }

        private void bd_things()
        {
            string connectionString = @"Data Source=LAPTOP-TT8JMRC0\SQLEXPRESS;Initial Catalog=SSBD;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string queryString1 = "select distinct npip from frinfo order by npip;";
                SqlCommand cmd1 = new SqlCommand(queryString1, connection);
                DataTable tbl1 = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(cmd1);
                da.Fill(tbl1);


                materialComboBox1.DataSource = tbl1;
                materialComboBox1.DisplayMember = "npip";// столбец для отображения
                                                         //materialComboBox1.ValueMember = "id";//столбец с id

                queryString1 = "select distinct nsection from frinfo where npip=" + materialComboBox1.Text + " order by nsection;";
                cmd1 = new SqlCommand(queryString1, connection);
                DataTable tbl2 = new DataTable();
                da = new SqlDataAdapter(cmd1);
                da.Fill(tbl2);


                materialComboBox2.DataSource = tbl2;
                materialComboBox2.DisplayMember = "nsection";

                queryString1 = "select distinct npoint from frinfo where npip=" + materialComboBox1.Text + " and nsection=" + materialComboBox2.Text + " order by npoint;";
                cmd1 = new SqlCommand(queryString1, connection);
                DataTable tbl3 = new DataTable();
                da = new SqlDataAdapter(cmd1);
                da.Fill(tbl3);


                materialComboBox3.DataSource = tbl3;
                materialComboBox3.DisplayMember = "npoint";

            }
        }
        double avg = 0;

        private void infoAVG()
        {
            string connectionString = @"Data Source=LAPTOP-TT8JMRC0\SQLEXPRESS;Initial Catalog=SSBD;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string queryString1 = "select AVG(freq) from avgfreq where npip=" + materialComboBox1.Text + " and nsection=" + materialComboBox2.Text + " and npoint=" + materialComboBox3.Text + ";";
                SqlCommand cmd1 = new SqlCommand(queryString1, connection);
                object returnVal = cmd1.ExecuteScalar();
                if (!DBNull.Value.Equals(returnVal))
                {

                    avg = Convert.ToDouble(returnVal);
                }
                else
                {
                    avg = 0;
                }

                connection.Close();
            }
        }

        private void chart_Load()
        {

            chart1.Series[0].Points.Clear();
            chart1.Series[1].Points.Clear();
            frlist.Clear();

            string connectionString = @"Data Source=LAPTOP-TT8JMRC0\SQLEXPRESS;Initial Catalog=SSBD;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                try
                {
                    string sql = "SELECT * FROM frinfo WHERE npip=" + materialComboBox1.Text + " and nsection=" + materialComboBox2.Text + " and npoint=" + materialComboBox3.Text + " order by npip, nsection, npoint, checktime;";
                    SqlCommand command = new SqlCommand(sql, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        int npip = reader.GetInt32(0);
                        int nsection = reader.GetInt32(1);
                        int npoint = reader.GetInt32(2);
                        DateTime dcheck = Convert.ToDateTime(reader.GetValue(3));
                        double freq = Convert.ToDouble(reader.GetValue(5));


                        FrPoint frpoint = new FrPoint(npip, nsection, npoint, dcheck, freq);
                        frlist.Add(frpoint);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
            }

            infoAVG();

            for (int i = 0; i < frlist.Count; i++)
            {


                chart1.Series["Series2"].Points.AddXY(frlist[i].dcheck, avg);
                chart1.Series["Series1"].Points.AddXY(frlist[i].dcheck, frlist[i].freq);



            }

            chart1.ChartAreas[0].AxisY.Maximum = frlist.Select(a => a.freq).Max() + 10;
            chart1.ChartAreas[0].AxisY.Minimum = frlist.Select(a => a.freq).Min() - 10;

            if (avg > chart1.ChartAreas[0].AxisY.Maximum) chart1.ChartAreas[0].AxisY.Maximum = avg + 10;
            if (avg < chart1.ChartAreas[0].AxisY.Minimum) chart1.ChartAreas[0].AxisY.Minimum = avg - 10;

            chart1.Series[0].XValueType = ChartValueType.DateTime;

            chart1.ChartAreas[0].AxisX.LabelStyle.Format = "hh-mm";
            chart1.Series[0].XValueType = ChartValueType.DateTime;

            chart1.ChartAreas[0].AxisX.Maximum = 45;
            chart1.ChartAreas[0].AxisX.Maximum = frlist.Select(a => a.dcheck).Max().ToOADate();
            chart1.ChartAreas[0].AxisX.Minimum = frlist.Select(a => a.dcheck).Min().ToOADate();
            chart1.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Minutes;
            chart1.ChartAreas[0].AxisX.Interval = 10;

            chart1.ChartAreas[0].AxisY.LabelStyle.Format = "{0:0.0}";
            chart1.Series[0].MarkerSize = 6;
            chart1.Series[0].MarkerColor = Color.DeepPink;
            chart1.Series[0].MarkerStyle = MarkerStyle.Circle;
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        double imscale = 1;


        private void loadImage()
        {
            label4.Visible = false;
            if (File.Exists(materialComboBox1.Text + "-" + materialComboBox2.Text + "-" + materialComboBox3.Text + ".bmp"))
            {
                try
                {
                    sc = 100;
                    imscale = 1;

                    using (var image = new Bitmap(materialComboBox1.Text + "-" + materialComboBox2.Text + "-" + materialComboBox3.Text + ".bmp"))
                    {
                        newImage = new Bitmap(image);
                    }
                    Bitmap b2 = new Bitmap(newImage);
                    string connectionString = @"Data Source=LAPTOP-TT8JMRC0\SQLEXPRESS;Initial Catalog=SSBD;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string queryString1 = "select AVG(imscale) from avgfreq where npip=" + materialComboBox1.Text + " and nsection=" + materialComboBox2.Text + " and npoint=" + materialComboBox3.Text + ";";
                        SqlCommand cmd1 = new SqlCommand(queryString1, connection);
                        object returnVal = cmd1.ExecuteScalar();

                        if (!DBNull.Value.Equals(returnVal))
                        {
                            imscale = Convert.ToDouble(returnVal);
                            sc = imscale * 100;

                            b2 = new Bitmap(newImage, new Size(Convert.ToInt32(newImage.Width * (imscale)), Convert.ToInt32(newImage.Height * (imscale))));
                            label4.Text = Convert.ToInt32(sc).ToString();
                        }
                        else
                        {
                            imscale = sc / 100;
                            label4.Text = Convert.ToInt32(sc).ToString();
                        }

                        connection.Close();
                    }

                    pictureBox1.Image = b2;
                    isimage = true;
                    pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                    pictureBox1.Dock = DockStyle.None;
                }
                catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            }
            else
            {
                pictureBox1.ImageLocation = "C:\\Users\\ccoo0\\source\\repos\\WindowsFormsApp2\\WindowsFormsApp2\\Properties\\imnull.png";
                isimage = false;
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                pictureBox1.Dock = DockStyle.Fill;
            }
            if (isimage)
                label4.Visible = true;
        }


        private void materialComboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (start == true)
            {
                chart_Load();
                loadImage();

            }
        }


        private void materialComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (start == true)
            {

                materialComboBox3.TabIndex = 0;
                materialComboBox3.Text = "";

                string connectionString = @"Data Source=LAPTOP-TT8JMRC0\SQLEXPRESS;Initial Catalog=SSBD;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string queryString1 = "select distinct npoint from frinfo where npip=" + materialComboBox1.Text + " and nsection=" + materialComboBox2.Text + " order by npoint;";
                    SqlCommand cmd1 = new SqlCommand(queryString1, connection);
                    DataTable tbl3 = new DataTable();
                    SqlDataAdapter da = new SqlDataAdapter(cmd1);
                    da.Fill(tbl3);


                    materialComboBox3.DataSource = tbl3;
                    materialComboBox3.DisplayMember = "npoint";
                }
                loadImage();
            }

        }

        private void materialComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (start == true) {

                materialComboBox2.TabIndex = 0;
                materialComboBox3.TabIndex = 0;
                materialComboBox2.Text = "";
                materialComboBox3.Text = "";

                string connectionString = @"Data Source=LAPTOP-TT8JMRC0\SQLEXPRESS;Initial Catalog=SSBD;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {

                    string queryString1 = "select distinct nsection from frinfo where npip=" + materialComboBox1.Text + " order by nsection;";
                    SqlCommand cmd1 = new SqlCommand(queryString1, connection);
                    DataTable tbl2 = new DataTable();
                    SqlDataAdapter da = new SqlDataAdapter(cmd1);
                    da.Fill(tbl2);
                    materialComboBox2.DataSource = tbl2;
                    materialComboBox2.DisplayMember = "nsection";


                }
                loadImage();

            }
        }

        private void materialButton1_Click(object sender, EventArgs e)
        {
            bd_things();
        }

        Image newImage = new Bitmap(300, 300);
        bool isimage = false;
        double sc = 100;

        private void materialButton2_Click(object sender, EventArgs e)
        {
            try
            {
                string path = "";
                Stream fileStream;
                using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
                {
                    openFileDialog.InitialDirectory = "c:\\";
                    openFileDialog.Filter = "Pdf Files|*.pdf";
                    openFileDialog.RestoreDirectory = true;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        path = openFileDialog.FileName;
                        fileStream = openFileDialog.OpenFile();
                        using (var pdf = new PdfDocument(fileStream))
                        {
                            PdfDrawOptions options = PdfDrawOptions.Create();
                            options.BackgroundColor = new PdfRgbColor(255, 255, 255);
                            options.HorizontalResolution = 300;
                            options.VerticalResolution = 300;
                            pdf.Pages[0].Save(materialComboBox1.Text + "-" + materialComboBox2.Text + "-" + materialComboBox3.Text + ".bmp", options);
                            Thread.Sleep(2000);
                            sc = 100;


                            using (var image = new Bitmap(materialComboBox1.Text + "-" + materialComboBox2.Text + "-" + materialComboBox3.Text + ".bmp"))
                            {
                                newImage = new Bitmap(image, image.Size);
                            }
                            Bitmap b2 = new Bitmap(newImage);
                            label4.Text = Convert.ToInt32(sc).ToString();
                            imscale = sc / 100;
                            pictureBox1.Image = b2;
                            isimage = true;
                            label4.Visible = true;
                        }

                        pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                        pictureBox1.Dock = DockStyle.None;
                    }
                }
            }
            catch
            {
                MessageBox.Show("Incorrect pdf");
            }
        }

        private void materialButton3_Click(object sender, EventArgs e)
        {
            if (isimage == true && sc > 5)
            {
                Bitmap b2 = new Bitmap(newImage, new Size(Convert.ToInt32(pictureBox1.Image.Width * 0.8), Convert.ToInt32(pictureBox1.Image.Height * 0.8)));
                pictureBox1.Image = b2;
                sc *= 0.8;
                imscale = sc / 100;
                label4.Text = Convert.ToInt32(sc).ToString();
            }
        }

        private void materialButton4_Click(object sender, EventArgs e)
        {
            if (isimage == true && sc < 200)
            {
                Bitmap b2 = new Bitmap(newImage, new Size(Convert.ToInt32(pictureBox1.Image.Width * 1.2), Convert.ToInt32(pictureBox1.Image.Height * 1.2)));
                pictureBox1.Image = b2;
                sc *= 1.2;
                imscale = sc / 100;
                label4.Text = Convert.ToInt32(sc).ToString();
            }
        }

        private void materialButton5_Click(object sender, EventArgs e)
        {
            string fileName = materialComboBox1.Text + "-" + materialComboBox2.Text + "-" + materialComboBox3.Text + ".bmp";

            if (File.Exists(fileName))
            {
                try
                {
                    File.Delete(fileName);
                    pictureBox1.ImageLocation = "C:\\Users\\ccoo0\\source\\repos\\WindowsFormsApp2\\WindowsFormsApp2\\Properties\\imnull.png";
                    isimage = false;
                    pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                    pictureBox1.Dock = DockStyle.Fill;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            else
            {
                MessageBox.Show("Загрузите новую схему", "Ошибка");
            }
        }

        private void materialButton6_Click(object sender, EventArgs e)
        {
            if (isimage == true)
            {
                int id = materialComboBox3.SelectedIndex;
                start = false;
                for (int i = 0; i < materialComboBox3.Items.Count; i++)
                {
                    if (i != id)
                    {
                        materialComboBox3.SelectedIndex = i;
                        newImage.Save(materialComboBox1.Text + "-" + materialComboBox2.Text + "-" + materialComboBox3.Text + ".bmp");
                    }
                }
                materialComboBox3.SelectedIndex = id;
                start = true;
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                string path = "";
                Stream fileStream;
                using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
                {
                    openFileDialog.InitialDirectory = "c:\\";
                    openFileDialog.Filter = "Pdf Files|*.pdf";
                    openFileDialog.RestoreDirectory = true;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        path = openFileDialog.FileName;
                        fileStream = openFileDialog.OpenFile();
                        using (var pdf = new PdfDocument(fileStream))
                        {
                            PdfDrawOptions options = PdfDrawOptions.Create();
                            options.BackgroundColor = new PdfRgbColor(255, 255, 255);
                            options.HorizontalResolution = 300;
                            options.VerticalResolution = 300;
                            pdf.Pages[0].Save(materialComboBox1.Text + "-" + materialComboBox2.Text + "-" + materialComboBox3.Text + ".bmp", options);
                            Thread.Sleep(2000);
                            sc = 100;
                            using (var image = new Bitmap(materialComboBox1.Text + "-" + materialComboBox2.Text + "-" + materialComboBox3.Text + ".bmp"))
                            {
                                newImage = new Bitmap(image, image.Size);
                            }
                            Bitmap b2 = new Bitmap(newImage);
                            if (newImage.Width > 10000 || newImage.Height > 10000)
                            {
                                b2 = new Bitmap(newImage, new Size(newImage.Width / 10, newImage.Height / 10));
                                sc = 10;
                            }
                            else if (newImage.Width > 5000 || newImage.Height > 5000)
                            {
                                b2 = new Bitmap(newImage, new Size(newImage.Width / 5, newImage.Height / 5));
                                sc = 20;
                            }
                            else if (newImage.Width > 2000 || newImage.Height > 2000)
                            {
                                b2 = new Bitmap(newImage, new Size(newImage.Width / 2, newImage.Height / 2));
                                sc = 50;
                            }
                            pictureBox1.Image = b2;
                            isimage = true;
                            label4.Visible = true;
                        }

                        pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                        pictureBox1.Dock = DockStyle.None;
                    }
                }
            }
            catch
            {
                MessageBox.Show("Incorrect pdf");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Удалить схему текущей точки?", "Подтвердите действие", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                string fileName = materialComboBox1.Text + "-" + materialComboBox2.Text + "-" + materialComboBox3.Text + ".bmp";

                if (File.Exists(fileName))
                {
                    try
                    {
                        File.Delete(fileName);
                        pictureBox1.ImageLocation = "C:\\Users\\ccoo0\\source\\repos\\WindowsFormsApp2\\WindowsFormsApp2\\Properties\\imnull.png";
                        isimage = false;
                        pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                        pictureBox1.Dock = DockStyle.Fill;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                }
                else
                {
                    MessageBox.Show("Загрузите новую схему", "Ошибка");
                }
            }
            else if (dialogResult == DialogResult.No)
            {
                //...
            }

        }

        private void button4_Click(object sender, EventArgs e)
        {
            bd_things();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (isimage == true)
            {
                int id = materialComboBox3.SelectedIndex;
                start = false;
                for (int i = 0; i < materialComboBox3.Items.Count; i++)
                {
                    if (i != id)
                    {
                        materialComboBox3.SelectedIndex = i;
                        newImage.Save(materialComboBox1.Text + "-" + materialComboBox2.Text + "-" + materialComboBox3.Text + ".bmp");
                    }
                }
                materialComboBox3.SelectedIndex = id;
                start = true;
            }
            else
            {
                MessageBox.Show("Схема не выбрана", "Ошибка");
            }
        }
        private void changeAVG()
        {
            string connectionString = @"Data Source=LAPTOP-TT8JMRC0\SQLEXPRESS;Initial Catalog=SSBD;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string queryString1 = "select avg(npip) from avgfreq where npip=" + materialComboBox1.Text + " and nsection=" + materialComboBox2.Text + " and npoint=" + materialComboBox3.Text + ";";
                SqlCommand cmd1 = new SqlCommand(queryString1, connection);
                object returnVal = cmd1.ExecuteScalar();
                if (!DBNull.Value.Equals(returnVal))
                {

                    queryString1 = "update avgfreq " + "set freq=" + avg.ToString().Replace(',', '.') + " where npip=" + materialComboBox1.Text + " and nsection=" + materialComboBox2.Text + " and npoint=" + materialComboBox3.Text + ";";
                    cmd1 = new SqlCommand(queryString1, connection);
                    returnVal = cmd1.ExecuteNonQuery();
                }
                else
                {
                    queryString1 = "insert into avgfreq values(" + materialComboBox1.Text + ", " + materialComboBox2.Text + ", " + materialComboBox3.Text + ", " + avg.ToString().Replace(',', '.') + ", null);";
                    cmd1 = new SqlCommand(queryString1, connection);
                    returnVal = cmd1.ExecuteNonQuery();
                }

                connection.Close();
            }
        }


        private void button5_Click(object sender, EventArgs e)
        {
            string connectionString = @"Data Source=LAPTOP-TT8JMRC0\SQLEXPRESS;Initial Catalog=SSBD;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string queryString1 = "select AVG(freq) from frinfo where npip=" + materialComboBox1.Text + " and nsection=" + materialComboBox2.Text + " and npoint=" + materialComboBox3.Text + ";";
                SqlCommand cmd1 = new SqlCommand(queryString1, connection);
                object returnVal = cmd1.ExecuteScalar();
                avg = Convert.ToDouble(returnVal);

                connection.Close();
            }
            changeAVG();
            chart_Load();
            MessageBox.Show("Ожидаемая частота точки равна " + avg.ToString(), "Калибровка завершена");
        }
        bool textmode = false;
        private void button10_Click(object sender, EventArgs e)
        {
            button10.Visible = false;
            textBox1.Visible = true;
            button11.Visible = true;
            textmode = true;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            button10.Visible = true;
            textBox1.Visible = false;
            button11.Visible = false;
            textmode = false;
            try
            {
                avg = Convert.ToDouble(textBox1.Text);
                changeAVG();
                chart_Load();
            }
            catch
            {
                MessageBox.Show("Некорректный формат ввода", "Ошибка");
            }
        }

        private void button11_VisibleChanged(object sender, EventArgs e)
        {
            if (button11.Visible == true)
            {
                materialComboBox1.Enabled = false;
                materialComboBox2.Enabled = false;
                materialComboBox3.Enabled = false;
            }

            else
            {
                materialComboBox1.Enabled = true;
                materialComboBox2.Enabled = true;
                materialComboBox3.Enabled = true;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string connectionString = @"Data Source=LAPTOP-TT8JMRC0\SQLEXPRESS;Initial Catalog=SSBD;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {

                connection.Open();
                string queryString1 = "select avg(npip) from avgfreq where npip=" + materialComboBox1.Text + " and nsection=" + materialComboBox2.Text + " and npoint=" + materialComboBox3.Text + ";";
                SqlCommand cmd1 = new SqlCommand(queryString1, connection);
                object returnVal = cmd1.ExecuteScalar();
                imscale += 0;
                imscale.ToString();
                if (!DBNull.Value.Equals(returnVal))
                {

                    queryString1 = "update avgfreq " + "set imscale=" + imscale.ToString().Replace(',', '.') + " where npip=" + materialComboBox1.Text + " and nsection=" + materialComboBox2.Text + " and npoint=" + materialComboBox3.Text + ";";
                    cmd1 = new SqlCommand(queryString1, connection);
                    returnVal = cmd1.ExecuteNonQuery();
                }
                else
                {
                    queryString1 = "insert into avgfreq values(" + materialComboBox1.Text + ", " + materialComboBox2.Text + ", " + materialComboBox3.Text + ", null," + imscale.ToString().Replace(',', '.') + ");";
                    cmd1 = new SqlCommand(queryString1, connection);
                    returnVal = cmd1.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        private void label4_TextChanged(object sender, EventArgs e)
        {
            if (label4.Text[label4.Text.Length - 1] != '%')
                label4.Text += '%';
        }

        private void label4_DoubleClick(object sender, EventArgs e)
        {
            textBox2.Visible = true;
            label5.Visible = true;
            label4.Visible = false;
            button12.Visible = true;
        }

        private void button12_VisibleChanged(object sender, EventArgs e)
        {
            if (button12.Visible == true)
            {
                materialButton4.Enabled = false;
                materialButton3.Enabled = false;
            }

            else
            {
                materialButton4.Enabled = true;
                materialButton3.Enabled = true;
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            try
            {
                sc = Convert.ToDouble(textBox2.Text);
                imscale = sc / 100;
                Bitmap b2 = new Bitmap(newImage, new Size(Convert.ToInt32(newImage.Width * (imscale)), Convert.ToInt32(newImage.Height * (imscale))));
                label4.Text = Convert.ToInt32(sc).ToString();
                pictureBox1.Image = b2;

            }
            catch
            {
                MessageBox.Show("Некорректный формат ввода", "Ошибка");
            }
            textBox2.Visible = false;
            label5.Visible = false;
            label4.Visible = true;
            button12.Visible = false;
        }

        private void materialButton1_Click_1(object sender, EventArgs e)
        {
            try
            {
                ecsp = Convert.ToDouble(textBox6.Text);
                frdevl.Clear();
                dataGridView2.Rows.Clear();
                string connectionString = @"Data Source=LAPTOP-TT8JMRC0\SQLEXPRESS;Initial Catalog=SSBD;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    //string queryString1 = "select lastfreq1.npip, lastfreq1.nsection, lastfreq1.npoint, lastfreq1.freq, avgfreq.freq, ROUND(abs(lastfreq1.freq-avgfreq.freq),2), lastfreq1.checktime from lastfreq1 join avgfreq on (lastfreq1.npip=avgfreq.npip and lastfreq1.nsection=avgfreq.nsection and lastfreq1.npoint=avgfreq.npoint) where ROUND(abs(lastfreq1.freq-avgfreq.freq),2)>1;";
                    string queryString1 = "select lastfreq1.npip, lastfreq1.nsection, lastfreq1.npoint, lastfreq1.freq, avgfreq.freq, ROUND(abs(lastfreq1.freq-avgfreq.freq),2), lastfreq1.checktime from lastfreq1 join avgfreq on (lastfreq1.npip=avgfreq.npip and lastfreq1.nsection=avgfreq.nsection and lastfreq1.npoint=avgfreq.npoint) where ROUND(abs(lastfreq1.freq-avgfreq.freq),2)>" + ecsp + ";";
                    SqlCommand cmd1 = new SqlCommand(queryString1, connection);
                    object returnVal = cmd1.ExecuteScalar();
                    SqlDataReader reader = cmd1.ExecuteReader();

                    while (reader.Read())
                    {
                        int npip = reader.GetInt32(0);
                        int nsection = reader.GetInt32(1);
                        int npoint = reader.GetInt32(2);

                        double freq = Convert.ToDouble(reader.GetValue(3));
                        double avgfreq = Convert.ToDouble(reader.GetValue(4));

                        double deviation = Convert.ToDouble(reader.GetValue(5));
                        DateTime dcheck = Convert.ToDateTime(reader.GetValue(6));
                        FrDev frdev = new FrDev(npip, nsection, npoint, freq, avgfreq, deviation, dcheck);
                        frdevl.Add(frdev);
                    }
                    connection.Close();
                }
                for (int i = 0; i < frdevl.Count; i++)
                    dataGridView2.Rows.Add(frdevl[i].npip, frdevl[i].nsection, frdevl[i].npoint, frdevl[i].freq, frdevl[i].avgfreq, frdevl[i].deviation, frdevl[i].dcheck.ToShortTimeString());
            }
            catch
            {
                MessageBox.Show("Некорректные данные", "Ошибка");
            }
        }

        private void materialButton2_Click_1(object sender, EventArgs e)
        {
            try
            {
                string connectionString = @"Data Source=LAPTOP-TT8JMRC0\SQLEXPRESS;Initial Catalog=SSBD;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    int npip1 = Convert.ToInt32(textBox3.Text);
                    int nsection1= Convert.ToInt32(textBox4.Text);
                    int npoint1 = Convert.ToInt32(textBox5.Text);
                    string queryString1 = "select lastfreq1.npip, lastfreq1.nsection, lastfreq1.npoint, lastfreq1.freq, avgfreq.freq, ROUND(abs(lastfreq1.freq-avgfreq.freq),2), lastfreq1.checktime from lastfreq1 join avgfreq on (lastfreq1.npip=avgfreq.npip and lastfreq1.nsection=avgfreq.nsection and lastfreq1.npoint=avgfreq.npoint) where lastfreq1.npip=" + npip1 + " and lastfreq1.nsection=" + nsection1 + " and lastfreq1.npoint=" + npoint1 +";";
                    SqlCommand cmd1 = new SqlCommand(queryString1, connection);
                    object returnVal = cmd1.ExecuteScalar();
                    SqlDataReader reader = cmd1.ExecuteReader();

                    if (returnVal == null)
                    {
                        MessageBox.Show("Оборудование указанной точки не было откалибровано", "Ошибка");
                    }
                    else
                    {

                        while (reader.Read())
                        {
                            int npip = reader.GetInt32(0);
                            int nsection = reader.GetInt32(1);
                            int npoint = reader.GetInt32(2);

                            double freq = Convert.ToDouble(reader.GetValue(3));
                            double avgfreq = Convert.ToDouble(reader.GetValue(4));

                            double deviation = Convert.ToDouble(reader.GetValue(5));

                            DateTime dcheck = Convert.ToDateTime(reader.GetValue(6));


                            FrDev frdev = new FrDev(npip, nsection, npoint, freq, avgfreq, deviation, dcheck);
                            frdevl1.Add(frdev);
                            int i = frdevl1.Count() - 1;
                            dataGridView1.Rows.Add(frdevl1[i].npip, frdevl1[i].nsection, frdevl1[i].npoint, frdevl1[i].freq, frdevl1[i].avgfreq, frdevl1[i].deviation, frdevl1[i].dcheck.ToShortTimeString());
                        }
                    }
                    connection.Close();
                }
                
            }
            catch
            {
                MessageBox.Show("Не удалось добавить точку", "Ошибка");
            }
        }

        private void materialButton5_Click_1(object sender, EventArgs e)
        {
            try
            {
                int npip1 = Convert.ToInt32(textBox3.Text);
                int nsection1 = Convert.ToInt32(textBox4.Text);
                int npoint1 = Convert.ToInt32(textBox5.Text);
                for (int i =0; i<dataGridView1.Rows.Count; i++)
                {
                    if (Convert.ToInt32(dataGridView1.Rows[i].Cells[0].Value) == npip1 &&
                        Convert.ToInt32(dataGridView1.Rows[i].Cells[1].Value) == nsection1 &&
                        Convert.ToInt32(dataGridView1.Rows[i].Cells[2].Value) == npoint1)
                    {
                        frdevl1.RemoveAt(i);
                        dataGridView1.Rows.RemoveAt(i);
                    }

                }

            }
            catch
            {
                MessageBox.Show("Не удалось удалить точку", "Ошибка");
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {

        }
        bool tab2if = false;
        double ecsp = 1;

        private void materialTabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            if (materialTabControl1.SelectedIndex == 1)
            {
                if (tab2if == false)
                {
                    frdevl.Clear();
                    dataGridView2.Rows.Clear();
                    string connectionString = @"Data Source=LAPTOP-TT8JMRC0\SQLEXPRESS;Initial Catalog=SSBD;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string queryString1 = "select lastfreq1.npip, lastfreq1.nsection, lastfreq1.npoint, lastfreq1.freq, avgfreq.freq, ROUND(abs(lastfreq1.freq-avgfreq.freq),2), lastfreq1.checktime from lastfreq1 join avgfreq on (lastfreq1.npip=avgfreq.npip and lastfreq1.nsection=avgfreq.nsection and lastfreq1.npoint=avgfreq.npoint) where ROUND(abs(lastfreq1.freq-avgfreq.freq),2)>" + ecsp + ";";
                        SqlCommand cmd1 = new SqlCommand(queryString1, connection);
                        object returnVal = cmd1.ExecuteScalar();
                        SqlDataReader reader = cmd1.ExecuteReader();

                        while (reader.Read())
                        {
                            int npip = reader.GetInt32(0);
                            int nsection = reader.GetInt32(1);
                            int npoint = reader.GetInt32(2);

                            double freq = Convert.ToDouble(reader.GetValue(3));
                            double avgfreq = Convert.ToDouble(reader.GetValue(4));

                            double deviation = Convert.ToDouble(reader.GetValue(5));

                            DateTime dcheck = Convert.ToDateTime(reader.GetValue(6));


                            FrDev frdev = new FrDev(npip, nsection, npoint, freq, avgfreq, deviation, dcheck);
                            frdevl.Add(frdev);
                        }

                        connection.Close();
                    }
                    for (int i = 0; i < frdevl.Count; i++)
                        dataGridView2.Rows.Add(frdevl[i].npip, frdevl[i].nsection, frdevl[i].npoint, frdevl[i].freq, frdevl[i].avgfreq, frdevl[i].deviation, frdevl[i].dcheck.ToShortTimeString());
                }
                tab2if = true;
            }
        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void button14_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < frdevl.Count; i++)
            {
                dataGridView1.Rows.Add(frdevl[i].npip, frdevl[i].nsection, frdevl[i].npoint, frdevl[i].freq, frdevl[i].avgfreq, frdevl[i].deviation, frdevl[i].dcheck.ToShortTimeString());
                FrDev frdevl1val = new FrDev (frdevl[i].npip, frdevl[i].nsection, frdevl[i].npoint, frdevl[i].freq, frdevl[i].avgfreq, frdevl[i].deviation, frdevl[i].dcheck);
                frdevl1.Add(frdevl1val);
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            while (dataGridView1.Rows.Count != 0)
            {
                dataGridView1.Rows.RemoveAt(0);
                frdevl1.RemoveAt(0);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            try
            {
                string path = "";
                using (System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    folderDialog.SelectedPath = "C:\\";

                    System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();
                    if (result.ToString() == "OK")
                        path = folderDialog.SelectedPath;
                }
                string s = materialComboBox1.Text + " " + materialComboBox2.Text + " " + materialComboBox3.Text + " " + DateTime.Now.ToString().Replace("{", "").Replace("}", "").Replace(".", "").Replace(":", "");
                StreamWriter f = new StreamWriter(path + "\\" + "result " + s.Trim() + ".csv");

                string connectionString = @"Data Source=LAPTOP-TT8JMRC0\SQLEXPRESS;Initial Catalog=SSBD;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string queryString1 = "select * from frinfo where npip=" + materialComboBox1.Text + " and nsection=" + materialComboBox2.Text + " and npoint=" + materialComboBox3.Text + ";";
                    SqlCommand cmd1 = new SqlCommand(queryString1, connection);
                    object returnVal = cmd1.ExecuteScalar();
                    SqlDataReader reader = cmd1.ExecuteReader();

                    int i = 0;

                    while (reader.Read())
                    {

                        int npip = reader.GetInt32(0);
                        int nsection = reader.GetInt32(1);
                        int npoint = reader.GetInt32(2);
                        DateTime dcheck = Convert.ToDateTime(reader.GetValue(3));

                        double freq = Convert.ToDouble(reader.GetValue(5));



                        int pos = dcheck.ToString().IndexOf(' ');
                        string res = dcheck.ToString().Substring(0, pos);

                        f.WriteLine(
                                dcheck.ToString() + ";" +
                                freq.ToString()
                                );

                        ++i;
                    }

                    connection.Close();
                    f.Close();

                }
            }
            catch
            {
                MessageBox.Show("Точка выбрана некорректно", "Ошибка");
            }

        }

        private void button16_Click(object sender, EventArgs e)
        {
            try
            {
                string path = "";
                using (System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    folderDialog.SelectedPath = "C:\\";

                    System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();
                    if (result.ToString() == "OK")
                        path = folderDialog.SelectedPath;
                }
                string s = DateTime.Now.ToString().Replace("{", "").Replace("}", "").Replace(".", "").Replace(":", "");
                StreamWriter f = new StreamWriter(path + "\\" + "result " + s.Trim() + ".csv");

                string connectionString = @"Data Source=LAPTOP-TT8JMRC0\SQLEXPRESS;Initial Catalog=SSBD;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";

                for (int i=0; i<frdevl1.Count; i++)
                {

                    int npip = frdevl1[i].npip;
                    int nsection = frdevl1[i].nsection;
                    int npoint = frdevl1[i].npoint;
                    DateTime dcheck = frdevl1[i].dcheck;
                    double freq = frdevl1[i].freq;
                    double avgfreq = frdevl1[i].avgfreq;
                    double deviation = frdevl1[i].deviation;
                    int pos = dcheck.ToString().IndexOf(' ');
                    string res = dcheck.ToString().Substring(0, pos);

                    f.WriteLine(
                        npip.ToString() + ";" +
                        nsection.ToString() + ";" +
                        npoint.ToString() + ";" +
                        avgfreq.ToString() + ";" +
                        freq.ToString() + ";" +
                        
                        deviation.ToString() + ";" +
                        dcheck.ToString()
                        );
                }
                f.Close();
            }
            catch
            {
                MessageBox.Show("Точка выбрана некорректно", "Ошибка");
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                string path = "";
                using (System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    folderDialog.SelectedPath = "C:\\";

                    System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();
                    if (result.ToString() == "OK")
                        path = folderDialog.SelectedPath;
                }
                string s = materialComboBox1.Text + " " + materialComboBox2.Text + " " + DateTime.Now.ToString().Replace("{", "").Replace("}", "").Replace(".", "").Replace(":", "");
                StreamWriter f = new StreamWriter(path + "\\" + "result " + s.Trim() + ".csv");

                string connectionString = @"Data Source=LAPTOP-TT8JMRC0\SQLEXPRESS;Initial Catalog=SSBD;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string queryString1 = "select * from frinfo where npip=" + materialComboBox1.Text + " and nsection=" + materialComboBox2.Text + " order by npoint;";
                    SqlCommand cmd1 = new SqlCommand(queryString1, connection);
                    object returnVal = cmd1.ExecuteScalar();
                    SqlDataReader reader = cmd1.ExecuteReader();

                    int i = 0;

                    while (reader.Read())
                    {

                        int npip = reader.GetInt32(0);
                        int nsection = reader.GetInt32(1);
                        int npoint = reader.GetInt32(2);
                        DateTime dcheck = Convert.ToDateTime(reader.GetValue(3));

                        double freq = Convert.ToDouble(reader.GetValue(5));
                        int pos = dcheck.ToString().IndexOf(' ');
                        string res = dcheck.ToString().Substring(0, pos);

                        f.WriteLine(
                                npoint.ToString() + ";" +
                                dcheck.ToString() + ";" +
                                freq.ToString()
                                );

                        ++i;
                    }

                    connection.Close();
                    f.Close();

                }
            }
            catch
            {
                MessageBox.Show("Точка выбрана некорректно", "Ошибка");
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            try
            {
                string path = "";
                using (System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    folderDialog.SelectedPath = "C:\\";

                    System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();
                    if (result.ToString() == "OK")
                        path = folderDialog.SelectedPath;
                }
                //string s = DateTime.Now.ToString().Replace("{", "").Replace("}", "").Replace(".", "").Replace(":", "");
                //StreamWriter f = new StreamWriter(path + "\\" + "result " + s.Trim() + ".csv");
                string s = materialComboBox1.Text + " "  + DateTime.Now.ToString().Replace("{", "").Replace("}", "").Replace(".", "").Replace(":", "");
                StreamWriter f = new StreamWriter(path + "\\" + "result " + s.Trim() + ".csv");

                string connectionString = @"Data Source=LAPTOP-TT8JMRC0\SQLEXPRESS;Initial Catalog=SSBD;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string queryString1 = "select * from frinfo where npip=" + materialComboBox1.Text + " order by nsection, npoint;";
                    SqlCommand cmd1 = new SqlCommand(queryString1, connection);
                    object returnVal = cmd1.ExecuteScalar();
                    SqlDataReader reader = cmd1.ExecuteReader();

                    int i = 0;

                    while (reader.Read())
                    {

                        int npip = reader.GetInt32(0);
                        int nsection = reader.GetInt32(1);
                        int npoint = reader.GetInt32(2);
                        DateTime dcheck = Convert.ToDateTime(reader.GetValue(3));

                        double freq = Convert.ToDouble(reader.GetValue(5));



                        int pos = dcheck.ToString().IndexOf(' ');
                        string res = dcheck.ToString().Substring(0, pos);

                        f.WriteLine(
                                nsection.ToString() + ";" +
                                npoint.ToString() + ";" +
                                dcheck.ToString() + ";" +
                                freq.ToString()
                                );

                        ++i;
                    }

                    connection.Close();
                    f.Close();

                }
            }
            catch
            {
                MessageBox.Show("Точка выбрана некорректно", "Ошибка");
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
                textBox7.Enabled = true;
                autoload = true;
                button15.Enabled = true;
                try
                {
                    interval = (int)(((double)Convert.ToInt32(textBox7.Text))*60000);
                    tick_start();
                }
                catch
                {
                    MessageBox.Show("Некорректный формат, интервал автоматически установлен на 5 минут", "Ошибка");
                    interval = 300000;
                    textBox7.Text = (((double)interval) / 60000).ToString();
                }
            }
            else
            {
                textBox7.Enabled = false;
                button15.Enabled = false;
                autoload = false;
                timer1.Stop();

            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            try
            {
                interval = (int)(((double)Convert.ToInt32(textBox7.Text)) * 60000);
            }
            catch
            {
                MessageBox.Show("Некорректный формат, интервал автоматически установлен на 5 минут", "Ошибка");
                interval = 300000;
                textBox7.Text = (((double)interval) / 60000).ToString();
            }

            timer1.Stop();
            tick_start();
        }

        private void button17_Click(object sender, EventArgs e)
        {
            try
            {
                string connectionString = @"Data Source=LAPTOP-TT8JMRC0\SQLEXPRESS;Initial Catalog=SSBD;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string queryString1 = "select * from pinfo where npip=" + textBox11.Text + " and nsection=" + textBox10.Text + " and npoint=" + textBox9.Text + ";";
                    SqlCommand cmd1 = new SqlCommand(queryString1, connection);
                    SqlDataReader reader = cmd1.ExecuteReader();

                    string address = "";
                    string name = "";
                    string pnum = "";
                    string pnum2 = "";
                    while (reader.Read())
                    {

                        address = reader.GetString(3);
                        name = reader.GetString(4);
                        pnum = reader.GetString(5);
                        pnum2 = reader.GetString(6);
                    }

                    connection.Close();
                    label20.Text = "Адрес: " + address;
                    label13.Text = "Ответственный: " + name;
                    label19.Text = "Телефон: " + pnum;
                    label21.Text = "Телефон бригады: " + pnum2;

                }

            }
            catch
            {

            }
        }

        private void materialComboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
