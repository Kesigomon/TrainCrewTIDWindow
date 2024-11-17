using Microsoft.VisualBasic;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace TrainCrewTIDWindow {

    public partial class TIDWindow : Form {

        /// <summary>
        /// �e�g���b�N�̐��̈ʒu��t�@�C�����Ȃǂ̃f�[�^
        /// </summary>
        private readonly List<LineSetting> lineSettingList;

        /// <summary>
        /// �e�g���b�N�̗�Ԕԍ��̈ʒu�Ȃǂ̃f�[�^�i�����ԗp�j
        /// </summary>
        private readonly List<NumberSetting> numSettingOut;

        /// <summary>
        /// �e�g���b�N�̗�Ԕԍ��̈ʒu�Ȃǂ̃f�[�^�i����ԗp�j
        /// </summary>
        private readonly List<NumberSetting> numSettingIn;

        /// <summary>
        /// ��Ԕԍ��̐F
        /// </summary>
        private readonly Dictionary<string, Color> numColor = [];

        /// <summary>
        /// ��Ԕԍ��ȊO�̐F
        /// </summary>
        private readonly Dictionary<string, Color> dicColor = [];

        /// <summary>
        /// �g���b�N�摜
        /// </summary>
        private readonly Dictionary<string, Image> lineImages = [];

        /// <summary>
        /// �T�[�o��TRAIN CREW�{�̂���擾�����O����H�̏��
        /// </summary>
        private readonly Dictionary<string, TrackData> trackDataList = [];

        /// <summary>
        /// �T�[�o����擾�����]�Q��̏��
        /// </summary>
        private readonly Dictionary<string, PointData> pointDataList = [];

        /// <summary>
        /// TRAIN CREW�{�̐ڑ��p
        /// </summary>
        private TrainCrewCommunication communication = new TrainCrewCommunication();

        /// <summary>
        /// �N�����w�i�摜
        /// </summary>
        private Image backgroundDefault;

        /// <summary>
        /// �ʏ펞�w�i�摜
        /// </summary>
        private Image backgroundImage;

        /// <summary>
        /// ��Ԕԍ������i�x���\������j
        /// </summary>
        private Image numLineL;

        /// <summary>
        /// ��Ԕԍ������i�x���\���Ȃ��j
        /// </summary>
        private Image numLineM;

        /// <summary>
        /// �^�s�ԍ�����
        /// </summary>
        private Image numLineS;

        /// <summary>
        /// �ԍ��t�H���g�摜
        /// </summary>
        private Image numberImage;

        /// <summary>
        /// �v�X�V���̊m�F�p
        /// </summary>
        private string states = "";

        /// <summary>
        /// �f�[�^�̎擾��
        /// </summary>
        private string source = "";

        /// <summary>
        /// �����Ƃ̎���
        /// </summary>
        private int timeDifference = -10;

        public TIDWindow() {
            InitializeComponent();

            backgroundDefault = Image.FromFile(".\\png\\Background-1.png");
            backgroundImage = Image.FromFile(".\\png\\Background.png");
            numLineL = Image.FromFile(".\\png\\TID_Retsuban_W_L.png");
            numLineM = Image.FromFile(".\\png\\TID_Retsuban_W_M.png");
            numLineS = Image.FromFile(".\\png\\TID_Retsuban_W_S.png");
            numberImage = Image.FromFile(".\\png\\Number.png");

            lineSettingList = LoadLineSetting("linedata.tsv");
            numSettingOut = LoadNumberSetting("number_outbound.tsv");
            numSettingIn = LoadNumberSetting("number_inbound.tsv");

            try {
                using var sr = new StreamReader(".\\setting\\color_setting.tsv");
                sr.ReadLine();
                var line = sr.ReadLine();
                while (line != null) {
                    var texts = line.Split('\t');
                    line = sr.ReadLine();

                    if (texts.Length < 4 || texts.Any(t => t == "")) {
                        continue;
                    }

                    if (texts[0].Length < 6) {
                        numColor.Add(texts[0], Color.FromArgb(int.Parse(texts[1]), int.Parse(texts[2]), int.Parse(texts[3])));
                    }
                    else {
                        dicColor.Add(texts[0], Color.FromArgb(int.Parse(texts[1]), int.Parse(texts[2]), int.Parse(texts[3])));
                    }
                }
            }
            catch {
            }

            try {
                using var sr = new StreamReader(".\\setting\\setting.txt");
                var line = sr.ReadLine();
                while (line != null) {
                    var texts = line.Split('=');
                    line = sr.ReadLine();

                    if (texts.Length < 2 || texts.Any(t => t == "")) {
                        continue;
                    }

                    switch (texts[0]) {
                        case "source":
                            source = texts[1];
                            break;
                    }
                }
            }
            catch {
            }

            pictureBox1.Image = new Bitmap(backgroundDefault);
            pictureBox1.Width = backgroundDefault.Width;
            pictureBox1.Height = backgroundDefault.Height;
            MaximumSize = new Size(backgroundDefault.Width + 16, backgroundDefault.Height + 39 + 24);
            Size = MaximumSize;


            // �����\��
            {
                using var g = Graphics.FromImage(pictureBox1.Image);
                foreach (var lineData in lineSettingList) {
                    if (lineData != null && lineData.IsDefault) {
                        AddImage(g, lineImages[lineData.FileNameR], lineData.PosX, lineData.PosY);
                    }
                }

                foreach (var numData in numSettingOut) {
                    if (numData != null && !numData.NotDraw) {
                        Image image = numData.Size switch {
                            NumberSize.L => new Bitmap(numLineL),
                            NumberSize.S => new Bitmap(numLineS),
                            _ => new Bitmap(numLineM),
                        };
                        var cm = new ColorMap {
                            OldColor = Color.White,
                            NewColor = Color.Red
                        };
                        var ia = new ImageAttributes();
                        ia.SetRemapTable([cm]);
                        AddImage(g, image, numData.PosX, numData.PosY + 10, ia);

                    }
                }

                foreach (var numData in numSettingIn) {
                    if (numData != null && !numData.NotDraw) {
                        Image image;
                        switch (numData.Size) {
                            case NumberSize.L:
                                image = new Bitmap(numLineL);
                                break;
                            case NumberSize.S:
                                image = new Bitmap(numLineS);
                                break;
                            default:
                                image = new Bitmap(numLineM);
                                break;
                        }
                        var cm = new ColorMap {
                            OldColor = Color.White,
                            NewColor = Color.Red
                        };
                        var ia = new ImageAttributes();
                        ia.SetRemapTable([cm]);
                        AddImage(g, image, numData.PosX, numData.PosY + 10, ia);

                    }
                }
            }


            if (source == "traincrew") {
                communication.ConnectionStatusChanged += UpdateConnectionStatus;
                communication.TCDataUpdated += UpdateTCData;
            }
            Task.Run(ClockUpdateLoop);
            Load += TIDWindow_Load;
        }

        /// <summary>
        /// �e�g���b�N�̐��̈ʒu��t�@�C�����Ȃǂ̃f�[�^��ǂݍ���
        /// </summary>
        /// <param name="fileName">�t�@�C����</param>
        /// <returns>�ǂݍ��񂾃f�[�^�̃��X�g</returns>
        private List<LineSetting> LoadLineSetting(string fileName) {
            List<LineSetting> list = [];
            try {
                using var sr = new StreamReader($".\\setting\\{fileName}");
                sr.ReadLine();
                var line = sr.ReadLine();
                var trackName = "";
                while (line != null) {
                    var texts = line.Split('\t');
                    line = sr.ReadLine();
                    var i = 1;
                    for (; i < texts.Length; i++) {
                        if (texts[i] == "") {
                            break;
                        }
                    }
                    if (i < 4) {
                        continue;
                    }
                    if (texts[0] != "") {
                        trackName = texts[0];
                    }
                    if (trackName == "") {
                        continue;
                    }
                    var imageName = texts[1];

                    if (i > 5) {
                        list.Add(new LineSetting(trackName, imageName, int.Parse(texts[2]), int.Parse(texts[3]), texts[4], texts[5] == bool.TrueString));
                    }
                    else {
                        list.Add(new LineSetting(trackName, imageName, int.Parse(texts[2]), int.Parse(texts[3])));
                    }
                    if (!lineImages.ContainsKey($"{imageName}_R")) {
                        lineImages[$"{imageName}_R"] = Image.FromFile($".\\png\\{imageName}_R.png");
                        lineImages[$"{imageName}_Y"] = Image.FromFile($".\\png\\{imageName}_Y.png");
                    }
                }
            }
            catch {
            }
            return list;
        }

        /// <summary>
        /// �e�g���b�N�̗�Ԕԍ��̈ʒu�Ȃǂ̃f�[�^��ǂݍ���
        /// </summary>
        /// <param name="fileName">�t�@�C����</param>
        /// <returns>�ǂݍ��񂾃f�[�^�̃��X�g</returns>
        private List<NumberSetting> LoadNumberSetting(string fileName) {
            List<NumberSetting> list = [];

            try {
                using var sr = new StreamReader($".\\setting\\{fileName}");
                sr.ReadLine();
                var line = sr.ReadLine();
                var trackName = "";
                while (line != null) {
                    var texts = line.Split('\t');
                    line = sr.ReadLine();
                    var i = 1;
                    for (; i < texts.Length; i++) {
                        if (texts[i] == "") {
                            break;
                        }
                    }
                    if (i < 4) {
                        continue;
                    }
                    if (texts[0] != "") {
                        trackName = texts[0];
                    }
                    if (trackName == "") {
                        continue;
                    }
                    NumberSize size;
                    switch (texts[1]) {
                        case "S":
                            size = NumberSize.S;
                            break;
                        case "M":
                            size = NumberSize.M;
                            break;
                        default:
                            size = NumberSize.L;
                            break;
                    }


                    if (i > 5) {
                        list.Add(new NumberSetting(trackName, size, int.Parse(texts[2]), int.Parse(texts[3]), texts[4], texts[5] == bool.TrueString));
                    }
                    else {
                        list.Add(new NumberSetting(trackName, size, int.Parse(texts[2]), int.Parse(texts[3])));
                    }
                }
            }
            catch {
            }
            return list;

        }

        /// <summary>
        /// ���W���w�肵�ĉ摜��\��t����
        /// </summary>
        /// <param name="g">TID�摜��Graphics</param>
        /// <param name="image">�\��t����摜</param>
        /// <param name="x">�\��t����x���W</param>
        /// <param name="y">�\��t����y���W</param>
        private void AddImage(Graphics g, Image image, int x, int y) {
            g.DrawImage(image, x, y, image.Width, image.Height);
        }

        /// <summary>
        /// ���W�ƐF���w�肵�ĉ摜��\��t����
        /// </summary>
        /// <param name="g">TID�摜��Graphics</param>
        /// <param name="image">�\��t����摜</param>
        /// <param name="x">�\��t����x���W</param>
        /// <param name="y">�\��t����y���W</param>
        /// <param name="ia">�F�̒u���������w�肵��ImageAttributes</param>
        private void AddImage(Graphics g, Image image, int x, int y, ImageAttributes ia) {
            g.DrawImage(image, new Rectangle(x, y, image.Width, image.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, ia);
        }

        /// <summary>
        /// ���W�ƐF���w�肵�ė�Ԕԍ��t�H���g�摜��\��t����i�S�p�j
        /// </summary>
        /// <param name="g">TID�摜��Graphics</param>
        /// <param name="isFullWidth">�S�p�ł��邩</param>
        /// <param name="numX">�摜���ɕ����������</param>
        /// <param name="numY">�摜���ɕ���������s</param>
        /// <param name="x">�\��t����x���W</param>
        /// <param name="y">�\��t����y���W</param>
        /// <param name="ia">�F�̒u���������w�肵��ImageAttributes</param>
        private void AddNumImage(Graphics g, bool isFullWidth, int numX, int numY, int x, int y, ImageAttributes ia) {
            g.DrawImage(numberImage, new Rectangle(x, y, isFullWidth ? 11 : 5, 9), 1 + numX * 6, 1 + numY * 10, isFullWidth ? 11 : 5, 9, GraphicsUnit.Pixel, ia);
        }

        /// <summary>
        /// ���W�ƐF���w�肵�ė�Ԕԍ��t�H���g�摜��\��t����
        /// </summary>
        /// <param name="g">TID�摜��Graphics</param>
        /// <param name="numX">�摜���ɕ����������</param>
        /// <param name="numY">�摜���ɕ���������s</param>
        /// <param name="x">�\��t����x���W</param>
        /// <param name="y">�\��t����y���W</param>
        /// <param name="ia">�F�̒u���������w�肵��ImageAttributes</param>
        private void AddNumImage(Graphics g, int numX, int numY, int x, int y, ImageAttributes ia) {
            AddNumImage(g, false, numX, numY, x, y, ia);
        }

        /// <summary>
        /// ���W�ƐF���w�肵�ė�Ԕԍ��t�H���g�摜��\��t����i�����̂݁j
        /// </summary>
        /// <param name="g">TID�摜��Graphics</param>
        /// <param name="num">����</param>
        /// <param name="x">�\��t����x���W</param>
        /// <param name="y">�\��t����y���W</param>
        /// <param name="ia">�F�̒u���������w�肵��ImageAttributes</param>
        private void AddNumImage(Graphics g, int num, int x, int y, ImageAttributes ia) {
            AddNumImage(g, num, 1, x, y, ia);
        }

        /// <summary>
        /// ���W���w�肵�ė�Ԕԍ��t�H���g�摜��\��t����i�S�p�j
        /// </summary>
        /// <param name="g">TID�摜��Graphics</param>
        /// <param name="isFullWidth">�S�p�ł��邩</param>
        /// <param name="numX">�摜���ɕ����������</param>
        /// <param name="numY">�摜���ɕ���������s</param>
        /// <param name="x">�\��t����x���W</param>
        /// <param name="y">�\��t����y���W</param>
        private void AddNumImage(Graphics g, bool isFullWidth, int numX, int numY, int x, int y) {
            g.DrawImage(numberImage, new Rectangle(x, y, isFullWidth ? 11 : 5, 9), 1 + numX * 6, 1 + numY * 10, isFullWidth ? 11 : 5, 9, GraphicsUnit.Pixel);
        }

        /// <summary>
        /// ���W���w�肵�ė�Ԕԍ��t�H���g�摜��\��t����
        /// </summary>
        /// <param name="g">TID�摜��Graphics</param>
        /// <param name="numX">�摜���ɕ����������</param>
        /// <param name="numY">�摜���ɕ���������s</param>
        /// <param name="x">�\��t����x���W</param>
        /// <param name="y">�\��t����y���W</param>
        private void AddNumImage(Graphics g, int numX, int numY, int x, int y) {
            AddNumImage(g, false, numX, numY, x, y);
        }

        /// <summary>
        /// ���W���w�肵�ė�Ԕԍ��t�H���g�摜��\��t����i�S�p�j
        /// </summary>
        /// <param name="g">TID�摜��Graphics</param>
        /// <param name="num">����</param>
        /// <param name="x">�\��t����x���W</param>
        /// <param name="y">�\��t����y���W</param>
        private void AddNumImage(Graphics g, int num, int x, int y) {
            AddNumImage(g, num, 1, x, y);
        }



        private async void TIDWindow_Load(object? sender, EventArgs? e) {

            switch (source) {
                case "traincrew":
                    //�����ɂ�all�̑��Atrackcircuit, signal, train���g���܂��B
                    communication.Request = ["trackcircuit"];
                    await communication.TryConnectWebSocket();
                    break;
                case "server":
                    //�f�t�H���g�̃T�[�o�ւ̐ڑ�����
                    break;
                default:
                    //�w�肵���C�ӂ̃T�[�o�ւ̐ڑ�����
                    break;
            }
        }


        private void UpdateConnectionStatus(string status) {
            label1.Text = status;
        }

        /// <summary>
        /// TRAIN CREW�{�̂���̃f�[�^���X�V���ꂽ�ۂɌĂ΂��
        /// </summary>
        /// <param name="tcData"></param>
        private void UpdateTCData(DataFromTrainCrew tcData) {
            var tcList = tcData.trackCircuitList;
            if (tcList == null) {
                return;
            }
            foreach (var tc in tcList) {
                if (trackDataList.ContainsKey(tc.Name)) {
                    trackDataList[tc.Name].SetStates(tc.On ? tc.Last : "", 2);
                }
                else {
                    trackDataList.Add(tc.Name, new TrackData(tc.Name, lineSettingList.Where(d => d.TrackName == tc.Name).ToArray(), numSettingOut.Where(d => d.TrackName == tc.Name).ToArray(), numSettingIn.Where(d => d.TrackName == tc.Name).ToArray(), tc.On ? tc.Last : "", 2));
                }
            }
            UpdateTID();
        }

        /// <summary>
        /// �K�v�ł����TID�̍ݐ��\�����X�V����
        /// �f�[�^���X�V���ꂽ�ۂ͂Ƃ肠����������Ă�
        /// </summary>
        public void UpdateTID() {

            // �O��ƑS��������Ԃł���Ε\���X�V���X�L�b�v����
            var nextStates = string.Join('/', trackDataList.Values.Select(d => d.ToString()));
            if (nextStates == states) {
                return;
            }
            states = nextStates;

            pictureBox1.Image = new Bitmap(backgroundImage);
            using var g = Graphics.FromImage(pictureBox1.Image);

            foreach (var track in trackDataList.Values) {
                if (!track.OnTrain && !track.IsReserved) {
                    continue;
                }

                // �g���b�N�̍ݐ��A�i�H�J�ʏ�ԕ\��

                var rule = "";
                foreach (var line in track.LineSettingArray) {
                    if (line == null) {
                        continue;
                    }

                    // �]�Q��̏�Ԃŕ\�������𔻒�
                    var r = line.PointName != "" ? $"{line.PointName}/{line.Reversed}" : "";
                    if (r != "" && rule == "" && pointDataList.ContainsKey(line.PointName)) {
                        var point = pointDataList[line.PointName];
                        if (point.IsLocked && point.IsReversed == line.Reversed) {
                            rule = r;
                        }
                    }

                    // �\�������𖞂����Ȃ��ꍇ�͕\�����Ȃ�
                    if (rule != r) {
                        continue;
                    }
                    AddImage(g, lineImages[track.OnTrain ? line.FileNameR : line.FileNameY], line.PosX, line.PosY);
                }
                if (!track.OnTrain) {
                    continue;
                }

                // ��ԕ\��

                var numHeader = Regex.Replace(track.Train, @"[0-9a-zA-Z]", "");  // ��Ԃ̓��̕����i��A���Ȃǁj
                _ = int.TryParse(Regex.Replace(track.Train, @"[^0-9]", ""), out var numBody);  // ��Ԗ{�́i���������j
                var numFooter = Regex.Replace(track.Train, @"[^a-zA-Z]", "");  // ��Ԃ̖����̕���

                var numSettingList = (numBody % 2 == 1 ? numSettingOut : numSettingIn).Where(d => d.TrackName == track.Name && !d.NotDraw && !d.ExistPoint);

                rule = "";
                foreach (var numData in (numBody % 2 == 1 ? track.NumSettingOut : track.NumSettingIn)) {
                    if (numData == null) {
                        continue;
                    }

                    // �]�Q��̏�Ԃŕ\�������𔻒�
                    var r = numData.PointName != "" ? $"{numData.PointName}/{numData.Reversed}" : "";
                    if (r != "" && rule == "" && pointDataList.ContainsKey(numData.PointName)) {
                        var point = pointDataList[numData.PointName];
                        if (point.IsLocked && point.IsReversed == numData.Reversed) {
                            rule = r;
                        }
                    }

                    // �\�������𖞂����Ȃ��ꍇ�͕\�����Ȃ�
                    if (rule != r) {
                        continue;
                    }

                    // �^��
                    if (numData.Size == NumberSize.S) {
                        var umban = numBody / 3000 * 100 + numBody % 100;

                        // �^�Ԃ������ɂ���E���ݒu
                        if (umban % 2 != 0) {
                            umban -= 1;
                            AddNumImage(g, 8, 0, numData.PosX, numData.PosY);
                        }
                        else {
                            AddNumImage(g, 9, 0, numData.PosX, numData.PosY);
                        }

                        // �^�Ԑݒu
                        for (var i = 2; i >= 0 && umban > 0; i--) {
                            var num = umban % 10;
                            AddNumImage(g, num, numData.PosX + 6 + i * 6, numData.PosY);
                            umban /= 10;
                        }
                        // �����ݒu
                        AddImage(g, numLineS, numData.PosX, numData.PosY + 10);
                    }
                    // ���
                    else {
                        var retsuban = numBody;

                        // ��ʐF
                        ImageAttributes? iaType = null;
                        foreach (var k in numColor.Keys) {
                            if ($"{numHeader}{numFooter}".Contains(k)) {
                                iaType = new ImageAttributes();
                                iaType.SetRemapTable([new ColorMap { OldColor = Color.White, NewColor = numColor[k] }]);
                                break;
                            }
                        }
                        // ��ʐF�����������Ȃ��ł���Εs���F��
                        if (iaType == null) {
                            iaType = new ImageAttributes();
                            if (retsuban <= 0 && dicColor.TryGetValue("UNKNOWN", out var newColor)) {
                                iaType.SetRemapTable([new ColorMap { OldColor = Color.White, NewColor = newColor }]);
                            }
                        }

                        // ��Ԃ̓��̕����ݒu
                        switch (numHeader) {
                            case "��":
                                AddNumImage(g, true, 0, 0, numData.PosX, numData.PosY, iaType);
                                break;
                            case "��":
                                AddNumImage(g, true, 2, 0, numData.PosX, numData.PosY, iaType);
                                break;
                            case "��":
                                AddNumImage(g, true, 4, 0, numData.PosX, numData.PosY, iaType);
                                break;
                        }

                        // ��Ԗ{�̐ݒu
                        for (var i = 3; i >= 0 && retsuban > 0; i--) {
                            var num = retsuban % 10;
                            AddNumImage(g, num, numData.PosX + 12 + i * 6, numData.PosY, iaType);
                            retsuban /= 10;
                        }


                        // ��Ԃ̖����̕����ݒu
                        if (numFooter.Length > 0) {
                            var x = GetAlphaX(numFooter[0]);
                            if (x < 55) {
                                AddNumImage(g, x, 2, numData.PosX + 36, numData.PosY, iaType);
                            }
                        }
                        if (numFooter.Length > 1) {
                            var x = GetAlphaX(numFooter[1]);
                            if (x < 55) {
                                AddNumImage(g, x, 2, numData.PosX + 42, numData.PosY, iaType);
                            }
                        }


                        // �x�������\���i�������̂��ߕK��0�E���F�j

                        /*var cm = new ColorMap();
                        cm.OldColor = Color.White;
                        cm.NewColor = Color.FromArgb(255, 0, 0);*/
                        var iaDelay = new ImageAttributes();
                        /*iaDelay.SetRemapTable([cm]);*/
                        if (numData.Size == NumberSize.L) {
                            AddNumImage(g, 0, numData.PosX + 54, numData.PosY, iaDelay);
                        }
                        Image numLineImage = numData.Size == NumberSize.L ? new Bitmap(numLineL) : new Bitmap(numLineM);
                        AddImage(g, numLineImage, numData.PosX, numData.PosY + 10, iaDelay);
                    }
                }
            }
        }

        /// <summary>
        /// ��ԉ摜���̃A���t�@�x�b�g�̗���W���擾����
        /// </summary>
        /// <param name="alpha">�A���t�@�x�b�g</param>
        /// <returns>��̍��W</returns>
        public int GetAlphaX(char alpha) {
            switch (alpha) {
                case 'A':
                    return 0;
                case 'B':
                    return 1;
                case 'C':
                    return 2;
                case 'K':
                    return 3;
                case 'S':
                    return 4;
                case 'T':
                    return 5;
                case 'X':
                    return 6;
                case 'Y':
                    return 7;
                case 'Z':
                    return 8;
                default:
                    return 9;
            }
        }

        private async void ClockUpdateLoop() {
            try {
                while (true) {
                    var timer = Task.Delay(10);
                    if (InvokeRequired) {
                        Invoke(new Action(UpdateClock));
                    }
                    else {
                        UpdateClock();
                    }
                    await timer;
                }
            }
            catch (ObjectDisposedException ex) {
            }
        }

        private void UpdateClock() {
            var time = DateTime.Now.AddHours(timeDifference);
            label2.Text = time.ToString("HH:mm:ss");
        }

        private void label2_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                timeDifference++;
            }
            else if(e.Button == MouseButtons.Right) {
                timeDifference--;
            }
        }
    }
}
