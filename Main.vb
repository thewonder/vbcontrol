Imports System.Security.Cryptography
Imports System.Text

Public Class Main
    Public SourcesButton(4) As RadioButton '信号源按钮数组
    Private SourceIDPage(Math.Ceiling((SourcesCount / 5) - 1), 4) As Integer  '信号源页面分配(多维数组存储各信号源ID)
    Public CurrentPage As Integer  '信号源当前页码
    Public PreSourceSelect(1) As Integer  '当前选择信号源号码 页数 选择第几个信号源

    Public RoomButton(RoomCount - 1) As RadioButton 'Room按钮数组
    Private RoomTabPage(RoomCount - 1) As TabPage 'Room对应面板
    Public PreRoomSelect As Integer
    Private MicroPhoneTrackBar(4) As MyVolumeBar.MyVolumeBar1
    Private MicroPhoneCheckBox(4) As CheckBox
    Private MicroPhoneGainMap() As Integer = {Room.MicroPhoneGainPoint.LecternMic, _
                                              Room.MicroPhoneGainPoint.WireLessMic01, _
                                              Room.MicroPhoneGainPoint.WireLessMic02, _
                                              Room.MicroPhoneGainPoint.WireLessMic03, _
                                              Room.MicroPhoneGainPoint.WireLessMic04}
    Private MicroPhoneMuteMap() As Integer = {Room.MicroPhoneMutePoint.LecternMic, _
                                              Room.MicroPhoneMutePoint.WireLessMic01, _
                                              Room.MicroPhoneMutePoint.WireLessMic02, _
                                              Room.MicroPhoneMutePoint.WireLessMic03, _
                                              Room.MicroPhoneMutePoint.WireLessMic04}

    Private DeviceCon As DeviceControl '设备通讯对象
    Private DocumentCam As DocumentCamera
    'zxg
    Public Enum DocumentCamActionType As Integer '收到指令后对应动作
        Freeze_DocumentCam = 0 'Freeze
        UnFreeze_DocumentCam = 1 'UnFreeze

    End Enum
    Private DocumentCam_actionArray As ArrayList


    Private WithEvents DocuemntCamEvents As DocumentCamera 'DocumentCamera 事件
    Private DocumentCameraFreezeCheck As Boolean = False

    'zxg
    Private Tv As TV
    Private WithEvents TVEvents As TV 'TV 事件
    Private isTvWakeup As Boolean = False
    Public Enum TVActionType As Integer '收到指令后对应动作
        OpenTV = 0 'open tv
        CloseTV = 1 'sleep tv
        CheckTV = 2 'initial check tv power state
        ShutdownTV = 3 'endsession 7min ,shutdown tv

    End Enum
    Private Tv_action As TVActionType    '
    Private Tv_actionArray As ArrayList
    Private isTvChecking As Boolean = False
    '


    Private PresenterCam As PresentCamera
    Private LightControlhttp As LightControlhttp

    Public ProjectorUse(1) As Integer  '模拟Projector放进哪种信号 SourceID

    Private Exam_Mode As Boolean = False  '考试模式开关
    Private Event_Mode As Boolean = False '。。。。开关

    Private OverFlowRoomButtons(PublicModule.OverFlowRoomCount - 1) As Button  'OverFlow房间按钮集合，方便用房间ID定位
    Private WithEvents OverFlowEvents As OverFlowRoom
    Public OverFlowSendMode As Boolean = False 'Overflow模式开关 用作多种判断
    Public OverFlowReceiveMode As Boolean = False

    Private WithEvents LightControlEvents As LightControl  '灯光控制事件
    Private WithEvents Projector1Events As ProjectorTcpControl '投影仪1事件
    Private WithEvents Projector2Events As ProjectorTcpControl '投影仪2事件

    Private RoomSettingTextbox(21) As TextBox '储存所有设置的TextBox
    Private RoomSettingTextboxSelect As Integer = -1 '储存被选中的TextBox索引
    Private RoomSettingTextboxCursor As Integer = -1 '光标位置

    Private MouseHook As SystemHook
    Private WithEvents MouseHookEvent As SystemHook

    Private MouseMoveOrNot As Boolean = False

    Private ExitNoTouchTimer As Threading.Thread

    'zxg
    'Private ExitNoTouchTimerTv As Threading.Thread
    Private NoResponse_TV_Timer As Threading.Thread


    Private Initialize As Boolean = True '初始化开关，用于开头EndSession不提问

    Public Sub New()

        ' 此调用是 Windows 窗体设计器所必需的。
        InitializeComponent()
        ' 在 InitializeComponent() 调用之后添加任何初始化。
        CheckProcess() '检查进程，防止二次打开
        BetaVersionCheck() '检查是否试用版
        CheckSourcesArray() '检查信号源数组
        PublicModule.OverFlowRoomInitialization() '实例化所有OverflowRoom
        Dim lc As New LoadXmlConfiguration(PublicModule.ConfigurationPath & "\Configuration.xml") '实例化读取信息类
        PublicModule.LightLoad = New LoadXmlLightSetting(PublicModule.ConfigurationPath & "\LightSetting.xml") '读取灯光指令
        PublicModule.LightCon = New LightControl(PublicModule.LightLoad.GetCommands.Item(PublicModule.ThisOverFlowRoomID))
        'DebugControl = New Debug
        PublicModule.DebugMessageFileCreate() '创建信息调试文件
        'DebugControl.Show()
        'DebugControl.Visible = False
        OverFlowSetEvents() '为Overflow绑定事件
        LightControlEvents = PublicModule.LightCon
        'Dim DTCPC As New DevicesTCPConnection '开始用TCP连接设备
        'DeviceCon = New DeviceControl() '实例化设备通讯类
        'DocumentCam = New DocumentCamera() '实例化
        'PresenterCam = New PresentCamera()
        If Isoverflowserver = 1 Then
            rbtnOverFlowSend.Visible = True
            rbtnOverFlowReceive.Visible = False
        Else
            rbtnOverFlowSend.Visible = False
            rbtnOverFlowReceive.Visible = True
        End If
        If Recordabledisplay = 1 Then
            lbProjector1.Text = "Recordable Display"
        Else
            lbProjector2.Text = "Recordable Display"
        End If
        'Initialization() '初始化
    End Sub

    Private Sub Main_FormClosed(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
        Try
            PublicModule.DebugMessageFileClose() '关闭信息文件调试流
            Dim MyOFR As OverFlowRoom = PublicModule.GetOverFlowRoomFromID(PublicModule.ThisOverFlowRoomID)
            If MyOFR.LineState Then '告诉服务器落线
                MyOFR.CommandSend(MyOFR.CommandMaker(OverFlowRoom.Commands.REQUEST_DISCONNECT_, PublicModule.ThisOverFlowRoomID))
                Threading.Thread.Sleep(1000)
            End If
        Catch ex As Exception
            End
        End Try
        End
    End Sub

    Private Sub CloseTouchPanel_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnCloseTouchPanel.Click
        Me.Close()
    End Sub

    Private Sub Main_Load_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Me.Hide()
        Dim DTCPC As New DevicesTCPConnection '开始用TCP连接设备
        DeviceCon = New DeviceControl(DTCPC) '实例化设备通讯类
        DocumentCam = New DocumentCamera() '实例化
        DocuemntCamEvents = DocumentCam
        'zxg
        Tv = New TV() '实例化
        TVEvents = Tv
        Tv_actionArray = New ArrayList

        PresenterCam = New PresentCamera()
        'OpenInfraredEquipmentSerialPort() '打开人体红外检查设备

        LightControlhttp = New LightControlhttp()

        Initialization() '初始化
        Me.Show()
        VolumeLayoutVisable(False)

        RoomButton_Click(rbtnEndSession, e)
    End Sub

    Private Sub CheckProcess() '检查是否重复启动
        Dim processes() As System.Diagnostics.Process = System.Diagnostics.Process.GetProcesses  '获取当前所有进程
        Dim process As System.Diagnostics.Process
        Dim i As Byte = 0
        For Each process In processes '进程迭代
            If String.Compare(process.ProcessName, System.Diagnostics.Process.GetCurrentProcess.ProcessName, False) = 0 Then
                i = i + 1
            End If
        Next
        If i > 1 Then
            MessageBox.Show("The TouchPanel has already run!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End
        End If
    End Sub

    Private Sub OverFlowSetEvents() '为Overflow绑定事件
        For i As Integer = 0 To PublicModule.OverFlowRoomCount - 1
            OverFlowEvents = PublicModule.GetOverFlowRoomFromID(i)
            AddHandler OverFlowEvents.StandAloneSet, AddressOf StandAloneSet
            AddHandler OverFlowEvents.WaitingSet, AddressOf WaitingSet
            AddHandler OverFlowEvents.SendingSet, AddressOf SendingSet
            AddHandler OverFlowEvents.ReceivingSet, AddressOf ReceivingSet
            AddHandler OverFlowEvents.RequestSendSuccess, AddressOf RequestSendSuccess
            AddHandler OverFlowEvents.AcceptReceiveSuccess, AddressOf AcceptReceiveSuccess
            AddHandler OverFlowEvents.RequestReceiveSuccess, AddressOf RequestReceiveSuccess
            AddHandler OverFlowEvents.RequestReceiveDisconnectSuccess, AddressOf RequestReceiveDisconnectSuccess
        Next

    End Sub

    Private Sub Initialization() '初始化
        SourcesButton(0) = rbtnSource1
        SourcesButton(1) = rbtnSource2
        SourcesButton(2) = rbtnSource3
        SourcesButton(3) = rbtnSource4
        SourcesButton(4) = rbtnSource5

        RoomButton(0) = rbtnEndSession
        RoomButton(1) = rbtnMicroPhones
        RoomButton(2) = rbtnScreensDisplays
        RoomButton(3) = rbtnLighting
        RoomButton(4) = rbtnOverFlow

        RoomTabPage(0) = Nothing
        RoomTabPage(1) = tapMicroPhones
        RoomTabPage(2) = tapScreensDisplays
        RoomTabPage(3) = tapLighting
        RoomTabPage(4) = tapOverFlow

        MicroPhoneTrackBar(0) = TrbLecternMic
        MicroPhoneTrackBar(1) = TrbWirelessMIC01
        MicroPhoneTrackBar(2) = TrbWirelessMIC02
        MicroPhoneTrackBar(3) = TrbWirelessMIC03
        MicroPhoneTrackBar(4) = TrbWirelessMIC04

        MicroPhoneCheckBox(0) = chbLecternMic
        MicroPhoneCheckBox(1) = chbWirelessMIC01
        MicroPhoneCheckBox(2) = chbWirelessMIC02
        MicroPhoneCheckBox(3) = chbWirelessMIC03
        MicroPhoneCheckBox(4) = chbWirelessMIC04

        CurrentPage = 0 '默认当前第0页

        btnPageUp.Enabled = False '关闭
        btnPageDown.Text = "More"

        tacFunction.Visible = False '默认隐藏功能面板
        lbCurrentFunctionTitle.Text = "" '功能面板标题为空
        'VolumeLayoutVisable(False)
        'tlpFunctionalPanel.ColumnStyles(1).Width = 0

        PreSourceSelect(0) = -1 '-1默认没有选择纪录
        PreSourceSelect(1) = -1

        PreRoomSelect = -1

        ProjectorUse(0) = -1
        ProjectorUse(1) = -1

        TrbVolume.MaxVolume = PublicModule.SourceGainMax   '根据AudioDSP增益设定
        TrbVolume.CurrentVolume = PublicModule.SourceGainDef
        TrbVolume.MinVolume = PublicModule.SourceGainMin
        TrbVolume.TickFrequency = (PublicModule.SourceGainMax - PublicModule.SourceGainMin) / 30
        'TrbVolume.TickFrequency = 12

        TrbLecternMic.MaxVolume = PublicModule.MicGainMax
        TrbLecternMic.CurrentVolume = PublicModule.MicGainDef
        TrbLecternMic.MinVolume = PublicModule.MicGainMin
        TrbLecternMic.TickFrequency = (PublicModule.MicGainMax - PublicModule.MicGainMin) / 30
        'TrbLecternMic.TickFrequency = 12

        TrbWirelessMIC01.MaxVolume = PublicModule.MicGainMax
        TrbWirelessMIC01.CurrentVolume = PublicModule.MicGainDef
        TrbWirelessMIC01.MinVolume = PublicModule.MicGainMin
        TrbWirelessMIC01.TickFrequency = (PublicModule.MicGainMax - PublicModule.MicGainMin) / 30
        'TrbWirelessMIC01.TickFrequency = 12

        TrbWirelessMIC02.MaxVolume = PublicModule.MicGainMax
        TrbWirelessMIC02.CurrentVolume = PublicModule.MicGainDef
        TrbWirelessMIC02.MinVolume = PublicModule.MicGainMin
        TrbWirelessMIC02.TickFrequency = (PublicModule.MicGainMax - PublicModule.MicGainMin) / 30
        'TrbWirelessMIC02.TickFrequency = 12

        TrbWirelessMIC03.MaxVolume = PublicModule.MicGainMax
        TrbWirelessMIC03.CurrentVolume = PublicModule.MicGainDef
        TrbWirelessMIC03.MinVolume = PublicModule.MicGainMin
        TrbWirelessMIC03.TickFrequency = (PublicModule.MicGainMax - PublicModule.MicGainMin) / 30
        'TrbWirelessMIC03.TickFrequency = 12

        TrbWirelessMIC04.MaxVolume = PublicModule.MicGainMax
        TrbWirelessMIC04.CurrentVolume = PublicModule.MicGainDef
        TrbWirelessMIC04.MinVolume = PublicModule.MicGainMin
        TrbWirelessMIC04.TickFrequency = (PublicModule.MicGainMax - PublicModule.MicGainMin) / 30
        'TrbWirelessMIC04.TickFrequency = 12

        For i As Integer = 0 To SourcesCount - 1 '实例化所有信号源类
            Dim S As New Source(GetSourcesName(i), i, DeviceCon)
            Sources(i) = S
        Next

        For i As Integer = 0 To SourceIDPage.GetLength(0) - 1 '把所有信号源ID按顺序存入页面
            For j As Integer = 0 To SourceIDPage.GetLength(1) - 1
                If ((i * (SourceIDPage.GetUpperBound(1) + 1)) + j) <= Sources.GetUpperBound(0) Then
                    SourceIDPage(i, j) = Sources((i * (SourceIDPage.GetUpperBound(1) + 1)) + j).SourceID
                End If
            Next
        Next

        For i As Integer = 0 To SourceIDPage.GetLength(1) - 1 '改变按钮文字
            SourcesButton(i).Text = Sources(SourceIDPage(CurrentPage, i)).SourceName '通过从页面获取ID,从而获得信号对象
        Next

        If CurrentPage = 0 Then 'PresenterCamera隐藏
            SourcesButton(4).Visible = PublicModule.PresenterCameraUse
        End If

        For i As Integer = 0 To RoomCount - 1 'Room类实例化
            Dim R As New Room(GetRoomName(i), i, DeviceCon)
            RoomClasses(i) = R
        Next

        For i As Integer = 1 To 12 'COM口选择
            cbxRoomSettingDocumentCameraCOM.Items.Add("COM" & i.ToString)
            cbxRoomSettingInfraredEquipmentCOM.Items.Add("COM" & i.ToString)
        Next

        Dim Buadrate(12) As String '波特率选择
        Buadrate(0) = "110"
        Buadrate(1) = "300"
        Buadrate(2) = "600"
        Buadrate(3) = "1200"
        Buadrate(4) = "1800"
        Buadrate(5) = "2400"
        Buadrate(6) = "4800"
        Buadrate(7) = "9600"
        Buadrate(8) = "14400"
        Buadrate(9) = "19200"
        Buadrate(10) = "28800"
        Buadrate(11) = "33600"
        Buadrate(12) = "56000"
        cbxRoomSettingDocumentCameraBuadrate.Items.AddRange(Buadrate)
        cbxRoomSettingInfraredEquipmentBuadrate.Items.AddRange(Buadrate)

        'zxg
        'For i As Integer = 0 To PublicModule.OverFlowRoomCount - 1
        '    cbxRoomSettingOverFlowRoomUse.Items.Add(PublicModule.GetRoomNameFromID(i))
        'Next
        cbxRoomSettingOverFlowRoomUse.Items.Add(PublicModule.GetRoomNameFromID(2))
        cbxRoomSettingOverFlowRoomUse.Items.Add(PublicModule.GetRoomNameFromID(3))

        FunctionalLayoutSave(0) = tlpFunctional.RowStyles(0).Height  '保存布局比例参数
        FunctionalLayoutSave(1) = tlpFunctional.RowStyles(1).Height
        For i As Integer = 0 To MainLayoutSave.GetUpperBound(0)
            MainLayoutSave(i) = tlpMain.ColumnStyles(i).Width
        Next
        VolumeLayoutSave = tlpMain.ColumnStyles(2).Width
        SourceLayoutSave = tlpMain.ColumnStyles(0).Width

        OverFlowRoomButtons(0) = btnCaseRoom1 '对应Overflow房间ID，将按钮加入数组
        OverFlowRoomButtons(1) = btnCaseRoom2
        OverFlowRoomButtons(2) = btnCaseRoom3
        OverFlowRoomButtons(3) = btnCaseRoom4
        OverFlowRoomButtons(4) = btnOGGB3
        OverFlowRoomButtons(5) = btnOGGB4
        OverFlowRoomButtons(6) = btnOGGB5
        OverFlowRoomButtons(7) = btn098
        OverFlowRoomButtons(8) = btnFAndP

        For i As Integer = 0 To PublicModule.OverFlowRoomCount - 1 '标识本OverFlow房间按钮
            If i = PublicModule.ThisOverFlowRoomID Then
                OverFlowRoomButtons(i).Text = PublicModule.GetRoomNameFromID(i) & vbCrLf & "(This Room)"
                OverFlowRoomButtons(i).BackgroundImage = My.Resources.NewSkin.Background_Grey
            Else
                OverFlowRoomButtons(i).Text = PublicModule.GetRoomNameFromID(i) & vbCrLf & "(Stand Alone)"
            End If
        Next

        rbtnOverFlowStandAlone.Checked = True '默认在StandAlone
        rbtnOverFlowModeSingle.Checked = True '默认在SingleMode


        For i As Integer = 0 To PublicModule.SourcesCount - 1 '开头所有信号源静音
            PublicModule.Sources(i).SetVolumeMuted(Source.AudioDSPMute.Muted)
        Next

        PublicModule.RoomClasses(4).PreviewMuted = True
        PublicModule.RoomClasses(4).SetVolumeMuted(Room.AudioDSPMute.Muted)

        RoomSettingTextbox(0) = tbxRoomSettingAudioDSPIP '通于记录设置菜单可输入TextBox的索引
        RoomSettingTextbox(1) = tbxRoomSettingAudioDSPPort
        RoomSettingTextbox(2) = tbxRoomSettingVideoSwitcherIP
        RoomSettingTextbox(3) = tbxRoomSettingVideoSwitcherPort
        RoomSettingTextbox(4) = tbxRoomSettingProjector1IP
        RoomSettingTextbox(5) = tbxRoomSettingProjector1Port
        RoomSettingTextbox(6) = tbxRoomSettingProjector2IP
        RoomSettingTextbox(7) = tbxRoomSettingProjector2Port
        RoomSettingTextbox(8) = tbxRoomSettingNoOneTouchExitTime
        RoomSettingTextbox(9) = tbxRoomSettingOverFlowServerIP
        RoomSettingTextbox(10) = tbxRoomSettingOverFlowServerPort
        RoomSettingTextbox(11) = tbxRoomSettingLightProjectName
        RoomSettingTextbox(12) = tbxRoomSettingLightControlIP
        RoomSettingTextbox(13) = tbxRoomSettingLightControlPort
        RoomSettingTextbox(14) = tbxRoomSettingLightFeedbackIP
        RoomSettingTextbox(15) = tbxRoomSettingLightFeedbackPort
        RoomSettingTextbox(16) = tbxRoomSettingPresenterCameraIP
        RoomSettingTextbox(17) = tbxRoomSettingPresenterCameraPort
        RoomSettingTextbox(18) = tbxRoomSettingProjector1ScreenIP
        RoomSettingTextbox(19) = tbxRoomSettingProjector1ScreenPort
        RoomSettingTextbox(20) = tbxRoomSettingProjector2ScreenIP
        RoomSettingTextbox(21) = tbxRoomSettingProjector2ScreenPort

        For i As Integer = 0 To RoomSettingTextbox.GetUpperBound(0)
            AddHandler RoomSettingTextbox(i).GotFocus, AddressOf SaveTheFoucusTextbox
        Next

        rbtnWhiteBoard.Checked = True '默认按下白板视频输出

        'DocumentCam.SendCommand(DocumentCamera.Action.LAMP_Status) 'DocumentCamera默认
        'DocumentCam.SendCommand(DocumentCamera.Action.Freeze_Status)
        DocumentCam.SendCommand(DocumentCamera.Action.POWER_ON)
        rbtnDocumentCameraLandscape.Checked = True
        rbtnDocumentCameraLightOFF.Checked = True
        rbtnDocumentCameraRelease.Checked = True

        'PresenterCamera默认设置
        'zxg ,steve comment 0402
        'PresenterCam.SendHttpCommand(PresentCamera.Action.Power_On)
        'PresenterCam.SendHttpCommand(PresentCamera.Action.Home_Position)
        'PresenterCam.SendHttpCommand(PresentCamera.Action.ZoomWide)
        'PresenterCam.SendHttpCommand(PresentCamera.Action.PlayBack)


        'TimeOut设置
        ExitNoTouchTimerReset()
        'Dim MouseMoveCheckThread As New Threading.Thread(AddressOf MouseMoveCheck)
        'MouseMoveCheckThread.Start()

        'MouseHook = New SystemHook(False, True)
        'MouseHookEvent = MouseHook

        'PublicModule.ExamModeExitTime = Now.AddSeconds(10) '调试时间测试
        ModeExitTimer.Enabled = True '打开模式退出检测Timer

        '投影仪事件帮定
        Projector1Events = PublicModule.Projector1Control
        Projector2Events = PublicModule.Projector2Control

        '投影仪电源状态查询
        ' adding another function to reset the connection
        'PublicModule.Projector1Control = New ProjectorTcpControl(PublicModule.DevicesTcpClient(PublicModule.DevicesName.Projector1), PublicModule.AllDevicesIPEndPoint(PublicModule.DevicesName.Projector1), ProjectorTcpControl.Projector.Projector1)
        PublicModule.Projector1Control.ProjectorCommandSent(ProjectorTcpControl.Action.Get_Power_Status)
        PublicModule.Projector2Control.ProjectorCommandSent(ProjectorTcpControl.Action.Get_Power_Status)

        ''zxg TV电源状态查询
        'Tv_action = TVActionType.CheckTV
        'Tv_actionArray.Add(TVActionType.CheckTV)
        'Tv.SendCommand(Tv.Action.CHECK)



        'Lighting初始状态
        rbtnLightWelcome.Checked = True
        rbtnLightPresets_Click(rbtnLightWelcome, New EventArgs)

        Dim tempThread As New Threading.Thread(AddressOf WhiteBoardHttpSend)
        tempThread.Start()

        '指定时间控制灯光到BlackOut
        'PublicModule.BlackOutTime = Now.AddSeconds(20)
        BlackOutTimer.Enabled = True

        AppearanceUpdate() '按照配置文件读取的配置更新外观
        'RoomButton_Click(rbtnEndSession, New System.EventArgs) '默认Muted 两个Projector

        DeviceCon.SendMessage("0*!", DevicesName.VideoMatrixSwitcher) '清除所有映射
        DeviceCon.SendMessage(Source.VedioInput.PTZRoomCamera & "*" & Source.VedioOuput.OverFlowSecondary & "!", DevicesName.VideoMatrixSwitcher) '输出PresenterCamera 到 Overflow

        '幕布收起
        DeviceCon.SendMessage("30 1" & vbCrLf, PublicModule.DevicesName.Projector1Screen)
        DeviceCon.SendMessage("30 1" & vbCrLf, PublicModule.DevicesName.Projector2Screen)

        PublicModule.Projector1Control.ProjectorCommandSent(ProjectorTcpControl.Action.AV_Mute_On)
        PublicModule.Projector2Control.ProjectorCommandSent(ProjectorTcpControl.Action.AV_Mute_On)
    End Sub

    Public Delegate Sub AppearanceUpdateDelegate() '外观更新委托，用于保存设置后更新
    Public Sub AppearanceUpdate() '按照配置文件读取的配置更新外观
        Dim MAU As New MainAppearanceUpdate(Me)
        MAU.UpdateAppearance()
    End Sub

    Private Sub rbtnSource_MouseClick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles rbtnSource1.MouseClick, _
                                                                                                          rbtnSource5.MouseClick, _
                                                                                                          rbtnSource4.MouseClick, _
                                                                                                          rbtnSource3.MouseClick, _
                                                                                                          rbtnSource2.MouseClick
        Dim i As Integer = 0
        Dim Number As Integer
        i = Array.IndexOf(SourcesButton, sender)


        Number = (CurrentPage * (SourceIDPage.GetUpperBound(1) + 1)) + i ' (（当前页码+1）*(第几个按钮+1)) -1 =第几个信号源
        lbCurrentFunctionTitle.Text = Sources(Number).SourceName '面板标题对应
        tacFunction.SelectedIndex = Sources(Number).SourceID '显示对应功能面板（对应信号源ID）
        tacFunction.Visible = True '显示面板

        'tlpVolume.Visible = False '先隐藏总音量
        chbVolumeMuted.Checked = Sources(Number).SourceMuted '显示此信号是否静音


        For Each s As Source In PublicModule.Sources '点哪一个就预览它的声音
            If s.SourceID = Number And Not Event_Mode Then
                If (s.SourceID = 0 Or s.SourceID = 1 Or s.SourceID = 5 Or s.SourceID = 6) And s.PreviewMaualMute Then

                Else
                    s.SetPreviewMute(Source.AudioDSPMute.UnMuted)
                    'chbLecternMuted.Checked = PublicModule.Sources(Number).PreviewMuted
                End If
            ElseIf s.SourceID <> Number And Not Event_Mode Then
                If (s.SourceID = 0 Or s.SourceID = 1 Or s.SourceID = 5 Or s.SourceID = 6) And s.PreviewMaualMute Then

                Else
                    s.SetPreviewMute(Source.AudioDSPMute.Muted)
                End If

            End If
        Next

        chbLecternMuted.Checked = PublicModule.Sources(Number).PreviewMuted '显示预览声音是否静音

        If (Number = 0 Or Number = 1 Or Number = 6) And Event_Mode Then
            'If (Number = 0 Or Number = 1) And Event_Mode Then 'Vv
            chbLecternMuted.Enabled = False
            chbVolumeMuted.Enabled = False
        Else
            chbLecternMuted.Enabled = True
            chbVolumeMuted.Enabled = True
        End If


        TrbVolume.CurrentVolume = Sources(Number).SourceVolume '显示此信号音量
        VolumeLayoutVisable(PublicModule.Sources(Number).GetVolumeVisable) '根据当前选择信号显示总音量
        Sources(Number).SwitchToThePreviewMonitor() '预览此信号源的信号

        If Number = 2 And Not rbtnDocumentCameraLightUpper.Checked Then '只要Document camera 的按键被选中后，document camera的灯光就要打开。不论有没有输出到投影机。直到End Session，你才把灯光关闭
            rbtnDocumentCameraLightUpper.Checked = True
            DocumentCameraRadioButton_Click(rbtnDocumentCameraLightUpper, New EventArgs)
        End If



        PreSourceSelect(0) = CurrentPage '纪录选择的页数
        PreSourceSelect(1) = i '纪录选择第几个信号源
        PreRoomSelect = -1
        RoomButtonUnCheck() '所有Room按钮弹起

        'ExitNoTouchTimerReset()
    End Sub

    Private Sub btnPageUp_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnPageUp.Click '向上翻页
        If CurrentPage >= 1 Then
            CurrentPage = CurrentPage - 1

            For i As Integer = 0 To SourceIDPage.GetLength(1) - 1 '改变按钮文字
                SourcesButton(i).Text = Sources(SourceIDPage(CurrentPage, i)).SourceName '通过从页面获取ID,从而获得信号对象
            Next

            btnPageDown.Visible = True
            btnPageDown.Enabled = True
            btnPageDown.BackgroundImage = PublicModule.GetSkinImage(PublicModule.CommonButtonColor)
            btnPageDown.Text = "More"

            SourceButtonUnCheck() '所有信号源按钮弹起
            SourceButtonHide() '隐藏多出来的信号源按钮

            If CurrentPage = 0 Then 'PresenterCamera隐藏
                SourcesButton(4).Visible = PublicModule.PresenterCameraUse
            End If
        End If

        If CurrentPage = 0 Then
            btnPageUp.Enabled = False
            btnPageUp.Visible = False
        End If

        If CurrentPage = PreSourceSelect(0) Then '恢复之前选择
            SourcesButton(PreSourceSelect(1)).Checked = True
        End If

        'lbSource.Text = "Source( Page" & (CurrentPage + 1).ToString & " )" '显示当前页码

        'ExitNoTouchTimerReset()
    End Sub

    Private Sub btnPageDown_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnPageDown.Click '向下翻页
        If CurrentPage < Math.Ceiling((SourcesCount / 5) - 1) Then
            CurrentPage = CurrentPage + 1

            For i As Integer = 0 To SourceIDPage.GetLength(1) - 1 '改变按钮文字
                SourcesButton(i).Text = Sources(SourceIDPage(CurrentPage, i)).SourceName '通过从页面获取ID,从而获得信号对象
            Next

            btnPageUp.Visible = True
            btnPageUp.Enabled = True
            btnPageUp.BackgroundImage = PublicModule.GetSkinImage(PublicModule.CommonButtonColor)
            btnPageUp.Text = "Back"

            SourceButtonUnCheck() '所有信号源按钮弹起
            SourceButtonHide() '隐藏多出来的信号源按钮

            If CurrentPage = 0 Then 'PresenterCamera隐藏
                SourcesButton(4).Visible = PublicModule.PresenterCameraUse
            End If

            If CurrentPage = 1 Then 'PresenterCamera隐藏
                SourcesButton(1).Visible = False
                SourcesButton(2).Visible = False
            End If

        End If

        If CurrentPage = Math.Ceiling((SourcesCount / 5) - 1) Then
            btnPageDown.Enabled = False
            btnPageDown.Visible = False
        End If

        If CurrentPage = PreSourceSelect(0) Then '恢复之前选择
            SourcesButton(PreSourceSelect(1)).Checked = True
        End If

        'lbSource.Text = "Source( Page" & (CurrentPage + 1).ToString & " )" '显示当前页码

        'ExitNoTouchTimerReset()
    End Sub

    Private Sub SourceButtonUnCheck() '所有信号源按钮弹起
        For Each temp As RadioButton In SourcesButton
            temp.Checked = False
            temp.BackgroundImage = PublicModule.GetSkinImage(CommonButtonColor) '皮肤更换
            temp.ForeColor = Color.White
        Next
    End Sub

    Private Sub RoomButtonUnCheck() '所有Room按钮弹起
        For Each temp As RadioButton In RoomButton
            temp.Checked = False
            temp.BackgroundImage = PublicModule.GetSkinImage(CommonButtonColor) '皮肤更换
            temp.ForeColor = Color.White

        Next
        PreRoomSelect = -1
        SourceLayoutVisable(True)
        ProjectorLayoutVisable(True)
    End Sub


    Private Sub SourceButtonHide() '隐藏多出来的信号源按钮
        If (SourcesCount Mod 5) = 0 Then '能被5整除就是刚好全部显示
            Exit Sub
        End If

        Dim temp As Byte = (Math.Ceiling(SourcesCount / 5) * 5) - SourcesCount '可显示个数（一定不足5个）

        If CurrentPage = Math.Ceiling((SourcesCount / 5) - 1) Then '当前的页数为最后一页
            For i As Integer = (5 - temp) To 4
                SourcesButton(i).Visible = False
            Next
        Else
            For i As Integer = 0 To 4
                SourcesButton(i).Visible = True
            Next
        End If
    End Sub

    '**********************************Volume*******************************************

    Private Sub TrbVolume_MouseUp(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TrbVolume.scValueChange  '总音量改变
        If tlpVolume.Visible Then '在可视的情况下(可改变总音量的信号源)才可改变
            If PreSourceSelect(0) <> -1 And PreSourceSelect(1) <> -1 And PreRoomSelect = -1 Then

                If TrbVolume.CurrentVolume <> Sources(SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))).SourceVolume Then '数值跟之前不一样才调整
                    Sources(SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))).ChangeVolume(TrbVolume.CurrentVolume)
                End If

            ElseIf PreRoomSelect <> -1 Then

                If PreRoomSelect = 1 Then '各路麦克风更变麦克风(仅限于麦克风画面)
                    Dim temp(4) As Integer

                    temp(0) = (TrbVolume.CurrentVolume - RoomClasses(PreRoomSelect).Volume) + TrbLecternMic.CurrentVolume
                    temp(1) = (TrbVolume.CurrentVolume - RoomClasses(PreRoomSelect).Volume) + TrbWirelessMIC01.CurrentVolume
                    temp(2) = (TrbVolume.CurrentVolume - RoomClasses(PreRoomSelect).Volume) + TrbWirelessMIC02.CurrentVolume
                    temp(3) = (TrbVolume.CurrentVolume - RoomClasses(PreRoomSelect).Volume) + TrbWirelessMIC03.CurrentVolume
                    temp(4) = (TrbVolume.CurrentVolume - RoomClasses(PreRoomSelect).Volume) + TrbWirelessMIC04.CurrentVolume

                    For i As Integer = 0 To temp.GetUpperBound(0)
                        If temp(i) > PublicModule.SourceGainMax Then
                            MicroPhoneTrackBar(i).CurrentVolume = PublicModule.SourceGainMax
                        ElseIf temp(i) < PublicModule.SourceGainMin Then
                            MicroPhoneTrackBar(i).CurrentVolume = PublicModule.SourceGainMin
                        Else
                            MicroPhoneTrackBar(i).CurrentVolume = temp(i)
                        End If
                    Next

                    RoomClasses(PreRoomSelect).Volume = TrbVolume.CurrentVolume

                End If

                RoomClasses(PreRoomSelect).Volume = TrbVolume.CurrentVolume
                TrbMicroPhone_MouseUp(TrbLecternMic, New System.EventArgs)

                For i As Integer = 0 To PublicModule.MicUse.GetUpperBound(0) '根据预设配置,不使用的麦克风不予调整音量
                    If PublicModule.MicUse(i) Then
                        Select Case i

                            Case 0
                                TrbMicroPhone_MouseUp(TrbWirelessMIC01, New System.EventArgs)
                            Case 1
                                TrbMicroPhone_MouseUp(TrbWirelessMIC02, New System.EventArgs)
                            Case 2
                                TrbMicroPhone_MouseUp(TrbWirelessMIC03, New System.EventArgs)
                            Case 3
                                TrbMicroPhone_MouseUp(TrbWirelessMIC04, New System.EventArgs)
                        End Select
                    End If
                Next

            End If

        End If

        'ExitNoTouchTimerReset()
    End Sub

    Private Sub chbVolumeMuted_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chbVolumeMuted.Click  '总音量静音
        If tlpVolume.Visible Then '在可视的情况下(可静音的信号源)才可改变

            If PreSourceSelect(0) <> -1 And PreSourceSelect(1) <> -1 And Not OverFlowReceiveMode Then
                If Event_Mode And Sources(SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))).SourceID <> 5 Then '在EventMode不允许操作
                    If chbVolumeMuted.Checked Then '返回前状态
                        chbVolumeMuted.Checked = False
                    Else
                        chbVolumeMuted.Checked = True
                    End If
                    Exit Sub
                End If
            End If


            If PreSourceSelect(0) <> -1 And PreSourceSelect(1) <> -1 And Not OverFlowReceiveMode Then
                If chbVolumeMuted.Checked Then
                    Sources(SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))).SetVolumeMuted(Source.AudioDSPMute.Muted)
                    Sources(SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))).MaualMute = False
                Else
                    Sources(SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))).SetVolumeMuted(Source.AudioDSPMute.UnMuted)
                    Sources(SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))).MaualMute = True
                End If
                'Sources(SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))).MaualMute = Not Sources(SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))).MaualMute
            End If

            If PreRoomSelect = 1 Then '当前选择为麦克风
                Dim CheckedState As Boolean = chbVolumeMuted.Checked
                RoomClasses(1).SetVolumeMuted(CheckedState)
                chbLecternMic.Checked = CheckedState
                chbWirelessMIC01.Checked = CheckedState
                chbWirelessMIC02.Checked = CheckedState
                chbWirelessMIC03.Checked = CheckedState
                chbWirelessMIC04.Checked = CheckedState
                MicroPhoneMute_Click(chbLecternMic, New System.EventArgs)

                For i As Integer = 0 To PublicModule.MicUse.GetUpperBound(0) '根据预设配置,不使用的麦克风不予音量静音
                    If PublicModule.MicUse(i) Then
                        Select Case i

                            Case 0
                                MicroPhoneMute_Click(chbWirelessMIC01, New System.EventArgs)
                            Case 1
                                MicroPhoneMute_Click(chbWirelessMIC02, New System.EventArgs)
                            Case 2
                                MicroPhoneMute_Click(chbWirelessMIC03, New System.EventArgs)
                            Case 3
                                MicroPhoneMute_Click(chbWirelessMIC04, New System.EventArgs)
                        End Select
                    End If
                Next

            End If

            If PreRoomSelect = 4 And OverFlowReceiveMode Then 'OverFlow接收
                If chbVolumeMuted.Checked Then
                    PublicModule.RoomClasses(4).SetVolumeMuted(Room.AudioDSPMute.Muted)
                Else
                    PublicModule.RoomClasses(4).SetVolumeMuted(Room.AudioDSPMute.UnMuted)
                End If

            End If

        End If

        'ExitNoTouchTimerReset()
    End Sub

    Private Sub chbLecternMuted_Click(ByVal sender As Object, ByVal e As EventArgs) Handles chbLecternMuted.Click
        If tlpVolume.Visible Then

            If PreSourceSelect(0) <> -1 And PreSourceSelect(1) <> -1 And Not OverFlowReceiveMode Then
                If Event_Mode And Sources(SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))).SourceID <> 5 Then '在EventMode不允许操作
                    If chbLecternMuted.Checked Then '返回前状态
                        chbLecternMuted.Checked = False
                    Else
                        chbLecternMuted.Checked = True
                    End If
                    Exit Sub
                End If
            End If


            If PreSourceSelect(0) <> -1 And PreSourceSelect(1) <> -1 And Not OverFlowReceiveMode Then
                If chbLecternMuted.Checked Then
                    Sources(SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))).SetPreviewMute(Source.AudioDSPMute.Muted)
                    Sources(SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))).PreviewMaualMute = True
                Else
                    Sources(SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))).SetPreviewMute(Source.AudioDSPMute.UnMuted)
                End If

            End If

            If OverFlowReceiveMode And PreRoomSelect = 4 Then
                If chbLecternMuted.Checked Then
                    PublicModule.RoomClasses(4).PreviewMuted = True
                Else
                    PublicModule.RoomClasses(4).PreviewMuted = False
                End If
            End If



        End If

        'ExitNoTouchTimerReset()
    End Sub


    '*********************************************************************************

    '*********************************Projector***************************************
    Private Sub btnProjector_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnProjector1.Click, _
                                                                                                       btnProjector2.Click  'Projector切换
        Dim ThisButton As Button = sender
        If PreSourceSelect(0) <> -1 And PreSourceSelect(1) <> -1 Then
            Dim ID As Integer = SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))

            If String.Compare(ThisButton.Name, btnProjector1.Name, False) = 0 Then

                If ID <> ProjectorUse(0) Then '选择信号与当前信号不同

                    If String.Compare(ThisButton.Text, "No Show", False) = 0 Then '如果之前是No Show，就unmute
                        DeviceCon.SendMessage(Source.VedioOuput.Projector1 & "*0B", DevicesName.VideoMatrixSwitcher)
                        PublicModule.Projector1Control.ProjectorCommandSent(ProjectorTcpControl.Action.Power_On)
                        chbProjector1Power.Checked = False
                        PublicModule.Projector1Control.ProjectorCommandSent(ProjectorTcpControl.Action.AV_Mute_Off)
                        PublicModule.Projector1Control.ProjectorCommandSent(ProjectorTcpControl.Action.Input_HDMI)
                        DeviceCon.SendMessage("33 1" & vbCrLf, PublicModule.DevicesName.Projector1Screen)
                        If PublicModule.LightCon.CurrentLightMode = LightControl.LightState.Welcome Then
                            rbtnLightTeach.Checked = True
                            rbtnLightPresets_Click(rbtnLightTeach, New EventArgs)
                        End If
                    End If


                    Sources(ID).VedioSwitch(Source.VedioOuput.Projector1)
                    ProjectorUse(0) = SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))

                    If OverFlowSendMode Then '如果是在Overlflow发送模式的话，信号跟投影机一样，影射到OverFlow输出
                        DeviceCon.SendMessage(Sources(ID).VedioInputMap(ID) & "*" & Source.VedioOuput.OverFlowPrimary & "!", DevicesName.VideoMatrixSwitcher)
                    End If

                    For i As Integer = 0 To SourcesCount - 1 '使用Projector输出的Source不静音,其余静音
                        If (Sources(i).SourceID = 0 Or Sources(i).SourceID = 1 Or Sources(i).SourceID = 5 Or Sources(i).SourceID = 6) And Not Sources(i).MaualMute Then '手动Mute过的不按规则

                            If Sources(i).SourceID = ProjectorUse(0) Then
                                Sources(i).SetVolumeMuted(Source.AudioDSPMute.UnMuted)
                                chbVolumeMuted.Checked = False
                            ElseIf Sources(i).SourceID = ProjectorUse(1) Then

                            Else
                                Sources(i).SetVolumeMuted(Source.AudioDSPMute.Muted)
                            End If

                        End If
                    Next

                    ThisButton.Text = Sources(ID).SourceName
                    OpenTV() 'zxg
                    ThisButton.BackgroundImage = PublicModule.GetSkinImage(PublicModule.SourceAndProjectorButtonPressColor) '皮肤更换
                    ThisButton.ForeColor = Color.Black

                Else '选择信号与当前信号相同

                    'DeviceCon.SendMessage(Source.VedioOuput.Projector1 & "*1B", DevicesName.VideoMatrixSwitcher)
                    DeviceCon.SendMessage("0*" & Source.VedioOuput.Projector1 & "!", DevicesName.VideoMatrixSwitcher)
                    DeviceCon.SendMessage("0*" & Source.VedioOuput.RecordingDigitizer & "!", DevicesName.VideoMatrixSwitcher)
                    ProjectorUse(0) = -1
                    btnProjector1.BackgroundImage = PublicModule.GetSkinImage(PublicModule.CommonButtonColor)
                    btnProjector1.ForeColor = Color.White
                    btnProjector1.Text = "No Show"
                    CloseTV() 'zxg
                    PublicModule.Projector1Control.ProjectorCommandSent(ProjectorTcpControl.Action.AV_Mute_On)

                    If ID <> ProjectorUse(1) Then '两个projector一样信号不取消
                        If (Sources(ID).SourceID = 0 Or Sources(ID).SourceID = 1 Or Sources(ID).SourceID = 5 Or Sources(ID).SourceID = 6) And Not Sources(ID).MaualMute Then '手动Mute过的不按规则

                            Sources(ID).SetVolumeMuted(Source.AudioDSPMute.Muted) '被取消者者静音
                            chbVolumeMuted.Checked = True

                        End If
                    End If

                    If OverFlowSendMode Then
                        DeviceCon.SendMessage("0*" & Source.VedioOuput.OverFlowPrimary & "!", DevicesName.VideoMatrixSwitcher)
                    End If

                End If

                If rbtnOverFlowModeSingle.Checked And Not btnProjector2.Enabled Then '在Overflow sigle mode模式,让projector2与projector1输出一致
                    btnProjector_Click(btnProjector2, New EventArgs)
                End If

            Else

                If ID <> ProjectorUse(1) Then '选择信号与当前信号不同


                    If String.Compare(ThisButton.Text, "No Show", False) = 0 Then '如果之前是No Show，就unmute
                        DeviceCon.SendMessage(Source.VedioOuput.Projector2 & "*0B", DevicesName.VideoMatrixSwitcher)
                        PublicModule.Projector2Control.ProjectorCommandSent(ProjectorTcpControl.Action.Power_On)
                        chbProjector2Power.Checked = False
                        PublicModule.Projector2Control.ProjectorCommandSent(ProjectorTcpControl.Action.AV_Mute_Off)
                        PublicModule.Projector2Control.ProjectorCommandSent(ProjectorTcpControl.Action.Input_HDMI)
                        DeviceCon.SendMessage("33 1" & vbCrLf, PublicModule.DevicesName.Projector2Screen)
                        If PublicModule.LightCon.CurrentLightMode = LightControl.LightState.Welcome Then
                            rbtnLightTeach.Checked = True
                            rbtnLightPresets_Click(rbtnLightTeach, New EventArgs)
                        End If
                    End If

                    Sources(ID).VedioSwitch(Source.VedioOuput.Projector2)
                    ProjectorUse(1) = SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))

                    If OverFlowSendMode And rbtnOverFlowModeDouble.Checked Then '如果是在Overlflow发送模式的话，信号跟投影机一样，影射到OverFlow输出
                        DeviceCon.SendMessage(Sources(ID).VedioInputMap(ID) & "*" & Source.VedioOuput.OverFlowSecondary & "!", DevicesName.VideoMatrixSwitcher)
                    End If

                    For i As Integer = 0 To SourcesCount - 1

                        If (Sources(i).SourceID = 0 Or Sources(i).SourceID = 1 Or Sources(i).SourceID = 5 Or Sources(i).SourceID = 6) And Not Sources(i).MaualMute Then '手动Mute过的不按规则

                            If Sources(i).SourceID = ProjectorUse(1) Then
                                Sources(i).SetVolumeMuted(Source.AudioDSPMute.UnMuted)
                                chbVolumeMuted.Checked = False
                            ElseIf Sources(i).SourceID = ProjectorUse(0) Then

                            Else
                                Sources(i).SetVolumeMuted(Source.AudioDSPMute.Muted)
                            End If

                        End If

                    Next

                    ThisButton.Text = Sources(ID).SourceName
                    ThisButton.BackgroundImage = PublicModule.GetSkinImage(PublicModule.SourceAndProjectorButtonPressColor) '皮肤更换
                    ThisButton.ForeColor = Color.Black


                Else '选择信号与当前信号相同
                    'DeviceCon.SendMessage(Source.VedioOuput.Projector2 & "*1B", DevicesName.VideoMatrixSwitcher)
                    'DeviceCon.SendMessage(Source.VedioOuput.RecordingDigitizer & "*1B", DevicesName.VideoMatrixSwitcher) '录制设备停止输出
                    DeviceCon.SendMessage("0*" & Source.VedioOuput.Projector2 & "!", DevicesName.VideoMatrixSwitcher)
                    ProjectorUse(1) = -1
                    btnProjector2.BackgroundImage = PublicModule.GetSkinImage(PublicModule.CommonButtonColor)
                    btnProjector2.ForeColor = Color.White
                    btnProjector2.Text = "No Show"
                    PublicModule.Projector2Control.ProjectorCommandSent(ProjectorTcpControl.Action.AV_Mute_On)

                    If ID <> ProjectorUse(0) Then '两个projector一样信号不取消

                        If (Sources(ID).SourceID = 0 Or Sources(ID).SourceID = 1 Or Sources(ID).SourceID = 5 Or Sources(ID).SourceID = 6) And Not Sources(ID).MaualMute Then '手动Mute过的不按规则

                            Sources(ID).SetVolumeMuted(Source.AudioDSPMute.Muted) '被取消者者静音
                            chbVolumeMuted.Checked = True

                        End If

                    End If

                    If OverFlowSendMode And rbtnOverFlowModeDouble.Checked Then
                        DeviceCon.SendMessage("0*" & Source.VedioOuput.OverFlowSecondary & "!", DevicesName.VideoMatrixSwitcher)
                    End If


                End If

            End If

        End If

        'ExitNoTouchTimerReset()
    End Sub

    Private Delegate Sub Projector1StateResponseDelegate(ByVal R As ProjectorTcpControl.Response) '投影仪1查询状态反馈Delegate
    Private Delegate Sub Projector2StateResponseDelegate(ByVal R As ProjectorTcpControl.Response) '投影仪2查询状态反馈Delegate

    Private Sub Projector1StateResponse(ByVal R As ProjectorTcpControl.Response) Handles Projector1Events.ProjectorStateResponse '投影仪1查询状态反馈
        Me.BeginInvoke(New Projector1StateResponseDelegate(AddressOf Projector1StateResponseInvoke), New Object() {R})
    End Sub

    Private Sub Projector2StateResponse(ByVal R As ProjectorTcpControl.Response) Handles Projector2Events.ProjectorStateResponse '投影仪2查询状态反馈
        Me.BeginInvoke(New Projector2StateResponseDelegate(AddressOf Projector2StateResponseInvoke), New Object() {R})
    End Sub

    Private Sub Projector1StateResponseInvoke(ByVal R As ProjectorTcpControl.Response) Handles Projector1Events.ProjectorStateResponse '投影仪1查询状态反馈Invoke

        Select Case R

            Case ProjectorTcpControl.Response.Audio_Mute_Off
            Case ProjectorTcpControl.Response.Audio_Mute_On
            Case ProjectorTcpControl.Response.AV_Mute_Off
            Case ProjectorTcpControl.Response.AV_Mute_On
            Case ProjectorTcpControl.Response.Input_Computer1
            Case ProjectorTcpControl.Response.Input_Computer2
            Case ProjectorTcpControl.Response.Input_S_Video
            Case ProjectorTcpControl.Response.Input_Video
            Case ProjectorTcpControl.Response.Lamp_GetStatus
            Case ProjectorTcpControl.Response.Power_CoolingDown
                chbProjector1Power.Enabled = False
                chbProjector1Power.Text = "Projector 1(Cooling Down)"
            Case ProjectorTcpControl.Response.Power_On
                chbProjector1Power.Checked = False
            Case ProjectorTcpControl.Response.Power_Standby
                chbProjector1Power.Checked = True
                chbProjector1Power.Enabled = True
                chbProjector1Power.Text = "Turn On Projector 1"
            Case ProjectorTcpControl.Response.Power_WarmUp
            Case ProjectorTcpControl.Response.Video_Mute_Off
            Case ProjectorTcpControl.Response.Video_Mute_On

        End Select

    End Sub

    Private Sub Projector2StateResponseInvoke(ByVal R As ProjectorTcpControl.Response) Handles Projector2Events.ProjectorStateResponse '投影仪2查询状态反馈Invoke

        Select Case R

            Case ProjectorTcpControl.Response.Audio_Mute_Off
            Case ProjectorTcpControl.Response.Audio_Mute_On
            Case ProjectorTcpControl.Response.AV_Mute_Off
            Case ProjectorTcpControl.Response.AV_Mute_On
            Case ProjectorTcpControl.Response.Input_Computer1
            Case ProjectorTcpControl.Response.Input_Computer2
            Case ProjectorTcpControl.Response.Input_S_Video
            Case ProjectorTcpControl.Response.Input_Video
            Case ProjectorTcpControl.Response.Lamp_GetStatus
            Case ProjectorTcpControl.Response.Power_CoolingDown
                chbProjector2Power.Enabled = False
                chbProjector2Power.Text = "Projector 2(Cooling Down)"
            Case ProjectorTcpControl.Response.Power_On
                chbProjector2Power.Checked = False
            Case ProjectorTcpControl.Response.Power_Standby
                chbProjector2Power.Checked = True
                chbProjector2Power.Enabled = True
                chbProjector2Power.Text = "Turn On Projector 2"
            Case ProjectorTcpControl.Response.Power_WarmUp
            Case ProjectorTcpControl.Response.Video_Mute_Off
            Case ProjectorTcpControl.Response.Video_Mute_On

        End Select

    End Sub


    Private ProjectorDelayCloseFinish As Boolean = False '是否完成延迟关闭

    Private Sub ProjectorDelayClose() '7分钟关投影仪 与EndSession
        ProjectorDelayCloseFinish = False

        If Not chbProjector1Power.Checked Then
            PublicModule.Projector1Control.ProjectorCommandSent(ProjectorTcpControl.Action.AV_Mute_On)
        End If

        If Not chbProjector2Power.Checked Then
            PublicModule.Projector2Control.ProjectorCommandSent(ProjectorTcpControl.Action.AV_Mute_On)
        End If

        Threading.Thread.Sleep(420000)

        If Not chbProjector1Power.Checked Then
            PublicModule.Projector1Control.ProjectorCommandSent(ProjectorTcpControl.Action.Power_Standby)
        End If

        If Not chbProjector2Power.Checked Then
            PublicModule.Projector2Control.ProjectorCommandSent(ProjectorTcpControl.Action.Power_Standby)
        End If

        '幕布收起
        DeviceCon.SendMessage("30 1" & vbCrLf, PublicModule.DevicesName.Projector1Screen)
        DeviceCon.SendMessage("30 1" & vbCrLf, PublicModule.DevicesName.Projector2Screen)

        'zxg

        Tv_action = TVActionType.ShutdownTV
        Tv_actionArray.Add(TVActionType.ShutdownTV)
        Tv.SendCommand(Tv.Action.CHECK)
        isTvChecking = True
        NoResponse_TV_Timer = New Threading.Thread(AddressOf NoResponse_TV)
        NoResponse_TV_Timer.Start(3000)


        ProjectorDelayCloseFinish = True
    End Sub


    '********************************************************************************

    '*********************************Room*******************************************

    Private Sub RoomButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles rbtnEndSession.Click, _
                                                                                                     rbtnMicroPhones.Click, _
                                                                                                     rbtnScreensDisplays.Click, _
                                                                                                     rbtnLighting.Click, _
                                                                                                     rbtnOverFlow.Click   'Room列按钮被按下
        Dim i As Integer = Array.IndexOf(RoomButton, sender)

        If String.Compare(RoomButton(i).Name, "rbtnEndSession", False) <> 0 Then '点击EndSession以外的按钮
            lbCurrentFunctionTitle.Text = RoomClasses(i).RoomName '显示标题
            tacFunction.SelectedTab = RoomTabPage(i) '显示对应面板
            tacFunction.Visible = True

            'tlpVolume.Visible = False
            TrbVolume.CurrentVolume = RoomClasses(i).Volume '音量读取,静音键状态读取
            TrbLecternMic.CurrentVolume = RoomClasses(i).LecternMicVolume
            chbVolumeMuted.Checked = RoomClasses(i).VolumeMuted
            chbLecternMic.Checked = RoomClasses(i).LecternMicMuted
            TrbWirelessMIC01.CurrentVolume = RoomClasses(i).WireLessMic01Volume
            chbWirelessMIC01.Checked = RoomClasses(i).WireLessMic01Muted
            TrbWirelessMIC02.CurrentVolume = RoomClasses(i).WireLessMic02Volume
            chbWirelessMIC02.Checked = RoomClasses(i).WireLessMic02Muted
            TrbWirelessMIC03.CurrentVolume = RoomClasses(i).WireLessMic03Volume
            chbWirelessMIC03.Checked = RoomClasses(i).WireLessMic03Muted
            TrbWirelessMIC04.CurrentVolume = RoomClasses(i).WireLessMic04Volume
            chbWirelessMIC04.Checked = RoomClasses(i).WireLessMic04Muted

            If i = 4 And OverFlowReceiveMode Then '不在接收Overflow模式下
                VolumeLayoutVisable(True)
                chbLecternMuted.Checked = RoomClasses(i).PreviewMuted
            Else
                VolumeLayoutVisable(RoomClasses(i).GetVolumeVisable)
            End If

            If i = 2 Then '投影仪控制
                PublicModule.Projector1Control.ProjectorCommandSent(ProjectorTcpControl.Action.Get_Power_Status)
                PublicModule.Projector2Control.ProjectorCommandSent(ProjectorTcpControl.Action.Get_Power_Status)
            End If


            If i = 1 Then

                If PreRoomSelect = -1 Or PreRoomSelect = 0 Then
                    'ProjectorLayoutVisable(False)
                    'SourceLayoutVisable(False)
                Else
                    'SourceLayoutVisable(False)
                End If

            ElseIf i = 2 And Not OverFlowReceiveMode Then

                If PreRoomSelect = -1 Or PreRoomSelect = 0 Then
                    'ProjectorLayoutVisable(False)
                Else
                    'SourceLayoutVisable(True)
                End If

            ElseIf i = 3 And Not OverFlowReceiveMode Then

                If PreRoomSelect = -1 Or PreRoomSelect = 0 Then
                    'ProjectorLayoutVisable(False)
                Else
                    'SourceLayoutVisable(True)
                End If

            ElseIf i = 4 And Not OverFlowReceiveMode Then

                If PreRoomSelect = -1 Or PreRoomSelect = 0 Then
                    'ProjectorLayoutVisable(False)
                Else
                    'SourceLayoutVisable(True)
                End If

            End If

            PreRoomSelect = i

            'ExitNoTouchTimerReset()

        Else '点击了EndSession按钮
            rbtnEndSession.Checked = True
            Dim ends As New ModeDialog(ModeDialog.Action.EndPresentation, Initialize)
            Initialize = False

            If ends.ShowDialog = Windows.Forms.DialogResult.Yes Then '显示对话框


                '延时关闭投影仪 包括了7分钟后的EndSession
                ProjectorDelayCloseThread = New Threading.Thread(AddressOf ProjectorDelayClose)
                ProjectorDelayCloseThread.Start()

                rbtnLightWelcome.Checked = True '返回WelcomeMode
                rbtnLightPresets_Click(rbtnLightWelcome, New EventArgs)

                Try
                    ExitNoTouchTimer.Abort() '关闭Timeout
                Catch ex As Exception

                End Try

                btnProjector1.Text = "No Show" '按钮显示没信号
                CloseTV() 'zxg
                btnProjector2.Text = "No Show"
                ProjectorUse(0) = -1
                ProjectorUse(1) = -1

                btnProjector1.BackgroundImage = PublicModule.GetSkinImage(PublicModule.CommonButtonColor)
                btnProjector2.BackgroundImage = PublicModule.GetSkinImage(PublicModule.CommonButtonColor)
                btnProjector1.ForeColor = Color.White
                btnProjector2.ForeColor = Color.White

                EndSession()

                Dim scr As New Screen(Screen.Action.EndSession) '显示屏幕
                If scr.ShowDialog = Windows.Forms.DialogResult.OK Then '屏幕返回后恢复各种状态
                    Me.Focus()
                    Me.WindowState = FormWindowState.Maximized
                    ExitNoTouchTimerReset() '无人触碰重新开始

                    If Not ProjectorDelayCloseFinish Then
                        ProjectorDelayCloseThread.Abort()
                    End If

                    NewSession()
                End If

            Else '选择不进入EndSession
                RoomButton(i).Checked = True
                rbtnEndSession.Checked = False
                'Exit Sub
                ExitNoTouchTimerReset()
            End If

        End If
    End Sub

    Private ProjectorDelayCloseThread As Threading.Thread

    Private Sub EndSession()

        DeviceCon.SendMessage("0*!", DevicesName.VideoMatrixSwitcher) '清除所有映射

        'DeviceCon.SendMessage(Source.VedioOuput.Projector1 & "*1B", DevicesName.VideoMatrixSwitcher) '两个Projector 都muted
        'DeviceCon.SendMessage(Source.VedioOuput.Projector2 & "*1B", DevicesName.VideoMatrixSwitcher)
        'DeviceCon.SendMessage(Source.VedioOuput.RecordingDigitizer & "*1B", DevicesName.VideoMatrixSwitcher) '录制设备停止输出
        'DeviceCon.SendMessage(Source.VedioOuput.PreviewMonitor & "*1B", DevicesName.VideoMatrixSwitcher) '预览显示器停止输出
        DeviceCon.SendMessage(Source.VedioInput.PTZRoomCamera & "*" & Source.VedioOuput.OverFlowSecondary & "!", DevicesName.VideoMatrixSwitcher) '输出PresenterCamera 到 Overflow

        'zxg  disconnect
        If (OverFlowReceiveMode) Then
            btnOverFlowEngagedDisconnect_Click(btnOverFlowEngagedDisconnect, New EventArgs)
        End If

        tacFunction.Visible = False '隐藏功能版
        lbCurrentFunctionTitle.Text = Nothing '不显示面板标题
        VolumeLayoutVisable(False)

        PreSourceSelect(0) = -1 '取消上次信号源点击记录
        PreSourceSelect(1) = -1



        For Each s As Source In PublicModule.Sources
            s.ChangeVolume(PublicModule.SourceGainDef)
            s.SetVolumeMuted(Source.AudioDSPMute.Muted)
            s.SetPreviewMute(Source.AudioDSPMute.Muted)
            s.MaualMute = False
            s.PreviewMaualMute = False
        Next

        RoomClasses(1).SetMicroPhoneVolume(PublicModule.MicGainDef, Room.MicroPhoneGainPoint.LecternMic)
        RoomClasses(1).SetMicroPhoneVolume(PublicModule.MicGainDef, Room.MicroPhoneGainPoint.WireLessMic01)
        RoomClasses(1).SetMicroPhoneVolume(PublicModule.MicGainDef, Room.MicroPhoneGainPoint.WireLessMic02)
        RoomClasses(1).SetMicroPhoneVolume(PublicModule.MicGainDef, Room.MicroPhoneGainPoint.WireLessMic03)
        RoomClasses(1).SetMicroPhoneVolume(PublicModule.MicGainDef, Room.MicroPhoneGainPoint.WireLessMic04)

        chbLecternMic.Checked = True
        chbWirelessMIC01.Checked = True
        chbWirelessMIC02.Checked = True
        chbWirelessMIC03.Checked = True
        chbWirelessMIC04.Checked = True
        MicroPhoneMute_Click(chbLecternMic, New System.EventArgs)
        MicroPhoneMute_Click(chbWirelessMIC01, New System.EventArgs)
        MicroPhoneMute_Click(chbWirelessMIC02, New System.EventArgs)
        MicroPhoneMute_Click(chbWirelessMIC03, New System.EventArgs)
        MicroPhoneMute_Click(chbWirelessMIC04, New System.EventArgs)

        PublicModule.RoomClasses(4).PreviewMuted = True
        PublicModule.RoomClasses(4).SetVolumeMuted(Room.AudioDSPMute.Muted)
        PublicModule.RoomClasses(4).Volume = PublicModule.SourceGainDef

        Dim MyOFR As OverFlowRoom = PublicModule.GetOverFlowRoomFromID(PublicModule.ThisOverFlowRoomID) '中断Overflow
        Select Case MyOFR.RoomState '根据各种情况而定

            Case OverFlowRoom.MyRoomState.Receiving
                btnOverFlowEngagedDisconnect_Click(btnOverFlowEngagedDisconnect, New EventArgs)
            Case OverFlowRoom.MyRoomState.Requesting
                MyOFR.CommandSend(MyOFR.CommandMaker(OverFlowRoom.Commands.REQUEST_BACK_TO_STANDLONE))
            Case OverFlowRoom.MyRoomState.Sending
                MyOFR.CommandSend(MyOFR.CommandMaker(OverFlowRoom.Commands.REQUEST_BACK_TO_STANDLONE))
            Case OverFlowRoom.MyRoomState.Waiting
                MyOFR.CommandSend(MyOFR.CommandMaker(OverFlowRoom.Commands.REQUEST_BACK_TO_STANDLONE))
        End Select

        'zxg
        rbtnOverFlowStandAlone.Checked = True
        OverFlowStaterbtnButon_CheckChange(rbtnOverFlowStandAlone, New EventArgs)
        StandAlone_Send_Receive_Click(rbtnOverFlowStandAlone, New EventArgs)

        rbtnOverFlowModeSingle.Checked = True 'OverFlow模式回到Single

        Select Case PreRoomSelect '正常显示被隐藏的界面

            Case 1
                'SourceLayoutVisable(True)
                'ProjectorLayoutVisable(True)
            Case 2
                'ProjectorLayoutVisable(True)
            Case 3
                'ProjectorLayoutVisable(True)
            Case 4
                'ProjectorLayoutVisable(True)
            Case Else

        End Select

        PreRoomSelect = 0
        SourceButtonUnCheck() '信号源按钮全部弹起

        If Event_Mode Then '关闭EventMode
            chbEventMode.Checked = False
            chbEventMode.Text = "Event Mode"
            rbtnMicroPhones.Visible = True
            Event_Mode = False
        End If

        If Exam_Mode Then '关闭ExamMode
            chbExamMode.Checked = False
            chbExamMode.Text = "Exam Mode"
            btnRoomSetting.BackgroundImage = PublicModule.GetSkinImage(PublicModule.CommonButtonColor)
            btnRoomSetting.ForeColor = Color.White

            Exam_Mode = False
        End If

        If Not btnRoomSetting.Visible Then '在RoomSetting界面也弹回去
            btnRoomSettingBack_Click(btnRoomSettingBack, New EventArgs)
        End If

        'DocumentCamera恢复初始状态
        'DocumentCam.SendCommand(DocumentCamera.Action.LAMP_OFF)
        'DocumentCam.SendCommand(DocumentCamera.Action.LAMP_Status)
        'If rbtnDocumentCameraFreeze.Checked Then
        ' DocumentCam.SendCommand(DocumentCamera.Action.Freeze)
        ' DocumentCam.SendCommand(DocumentCamera.Action.Freeze_Status)
        'End If
        'DocumentCam.SendCommand(DocumentCamera.Action.ROTATE_0)
        DocumentCam.SendCommand(DocumentCamera.Action.POWER_OFF)

        'PresenterCamera关闭
        'zxg steve comment 0402
        'PresenterCam.SendHttpCommand(PresentCamera.Action.Power_On)

        '白板
        WhiteBoardHttpSend("ClearAll")
        WhiteBoardHttpSend("EndSession")
        rbtnWhiteBoard.Checked = True
        AnnotationTabletButton_Click(rbtnWhiteBoard, New EventArgs)

    End Sub

    Private Sub NewSession()

        'For Each s As Source In PublicModule.Sources
        '    s.SetPreviewMute(Source.AudioDSPMute.UnMuted)
        'Next

        chbLecternMic.Checked = False
        MicroPhoneMute_Click(chbLecternMic, New System.EventArgs)

        For a As Integer = 0 To PublicModule.MicUse.GetUpperBound(0) '在EndSession恢复时,根据配置要求,不使用的麦克风不予恢复状态
            If PublicModule.MicUse(a) Then
                Select Case a

                    Case 0
                        chbWirelessMIC01.Checked = False
                        MicroPhoneMute_Click(chbWirelessMIC01, New System.EventArgs)
                    Case 1
                        chbWirelessMIC02.Checked = False
                        MicroPhoneMute_Click(chbWirelessMIC02, New System.EventArgs)
                    Case 2
                        chbWirelessMIC03.Checked = False
                        MicroPhoneMute_Click(chbWirelessMIC03, New System.EventArgs)
                    Case 3
                        chbWirelessMIC04.Checked = False
                        MicroPhoneMute_Click(chbWirelessMIC04, New System.EventArgs)
                End Select
            End If
        Next

        'DocumentCam.SendCommand(DocumentCamera.Action.POWER_ON)
        DocumentCam.SendCommand(DocumentCamera.Action.POWER_ON, 30000)
        rbtnDocumentCameraLandscape.Checked = True
        rbtnDocumentCameraLightOFF.Checked = True
        rbtnDocumentCameraRelease.Checked = True

        'PresenterCamera恢复初始状态
        'zxg ,steve comment 0402
        'PresenterCam.SendHttpCommand(PresentCamera.Action.Power_On)
        'PresenterCam.SendHttpCommand(PresentCamera.Action.Home_Position)
        'PresenterCam.SendHttpCommand(PresentCamera.Action.ZoomWide)
        'PresenterCam.SendHttpCommand(PresentCamera.Action.PlayBack)

        'Lighting初始状态
        rbtnLightWelcome.Checked = True
        rbtnLightPresets_Click(rbtnLightWelcome, New EventArgs)

        If ProjectorDelayCloseFinish Then '根据情况恢复投影仪
            chbProjector1Power.Checked = True
            chbProjector2Power.Checked = True
        Else
            'zxg, Steven comment
            ' PublicModule.Projector1Control.ProjectorCommandSent(ProjectorTcpControl.Action.AV_Mute_Off)
            ' PublicModule.Projector2Control.ProjectorCommandSent(ProjectorTcpControl.Action.AV_Mute_Off)
        End If


        RoomButtonUnCheck()

        '白板
        WhiteBoardHttpSend("NewSession")

        PublicModule.DebugNewMessageFile()

        'zxg TV电源状态查询 0505
        Tv_action = TVActionType.CheckTV
        Tv_actionArray.Add(TVActionType.CheckTV)
        Tv.SendCommand(Tv.Action.CHECK)
        isTvChecking = True
        NoResponse_TV_Timer = New Threading.Thread(AddressOf NoResponse_TV)
        NoResponse_TV_Timer.Start(3000)

    End Sub


    '********************************************************************************

    '***************************Micro-Phone******************************************

    Private Sub TrbMicroPhone_MouseUp(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TrbLecternMic.scValueChange, _
                                                                                                          TrbWirelessMIC01.scValueChange, _
                                                                                                          TrbWirelessMIC02.scValueChange, _
                                                                                                          TrbWirelessMIC03.scValueChange, _
                                                                                                          TrbWirelessMIC04.scValueChange    'Microphone面板改变音量
        Dim Trb As MyVolumeBar.MyVolumeBar1 = sender

        If PreRoomSelect = 1 Then
            Select Case Trb.Name
                Case TrbLecternMic.Name
                    If Trb.CurrentVolume <> RoomClasses(1).LecternMicVolume Then RoomClasses(1).SetMicroPhoneVolume(Trb.CurrentVolume, Room.MicroPhoneGainPoint.LecternMic)
                Case TrbWirelessMIC01.Name
                    If Trb.CurrentVolume <> RoomClasses(1).WireLessMic01Volume Then RoomClasses(1).SetMicroPhoneVolume(Trb.CurrentVolume, Room.MicroPhoneGainPoint.WireLessMic01)
                Case TrbWirelessMIC02.Name
                    If Trb.CurrentVolume <> RoomClasses(1).WireLessMic02Volume Then RoomClasses(1).SetMicroPhoneVolume(Trb.CurrentVolume, Room.MicroPhoneGainPoint.WireLessMic02)
                Case TrbWirelessMIC03.Name
                    If Trb.CurrentVolume <> RoomClasses(1).WireLessMic03Volume Then RoomClasses(1).SetMicroPhoneVolume(Trb.CurrentVolume, Room.MicroPhoneGainPoint.WireLessMic03)
                Case TrbWirelessMIC04.Name
                    If Trb.CurrentVolume <> RoomClasses(1).WireLessMic04Volume Then RoomClasses(1).SetMicroPhoneVolume(Trb.CurrentVolume, Room.MicroPhoneGainPoint.WireLessMic04)
            End Select
        End If

        'ExitNoTouchTimerReset()
    End Sub

    Private Sub TrbVolume_Scroll(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TrbVolume.MouseUp

    End Sub

    Private Sub MicroPhoneMute_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chbLecternMic.Click, _
                                                                                                         chbWirelessMIC01.Click, _
                                                                                                         chbWirelessMIC02.Click, _
                                                                                                         chbWirelessMIC03.Click, _
                                                                                                         chbWirelessMIC04.Click   '三个麦克风静音
        Dim i As Integer = Array.IndexOf(MicroPhoneCheckBox, sender)

        If MicroPhoneCheckBox(i).Checked Then
            RoomClasses(1).SetMicroPhoneMuted(MicroPhoneMuteMap(i), Room.AudioDSPMute.Muted)
        Else
            RoomClasses(1).SetMicroPhoneMuted(MicroPhoneMuteMap(i), Room.AudioDSPMute.UnMuted)
        End If

        If chbLecternMic.Checked And chbWirelessMIC01.Checked And chbWirelessMIC02.Checked And chbWirelessMIC03.Checked And chbWirelessMIC04.Checked Then
            'chbVolumeMuted.Checked = True
        Else
            'chbVolumeMuted.Checked = False
        End If

        'ExitNoTouchTimerReset()
    End Sub

    Private Sub btnMicroPhoneClose(ByVal sender As Object, ByVal e As EventArgs) Handles btnMicroPhonesBack.Click '关闭麦克风面板
        If PreSourceSelect(0) = -1 And PreSourceSelect(1) = -1 Then
            RoomButtonUnCheck()
            tacFunction.Visible = False
        Else
            tacFunction.SelectedIndex = SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))
        End If
        lbCurrentFunctionTitle.Text = ""
        SourceLayoutVisable(True)
        ProjectorLayoutVisable(True)
    End Sub


    '********************************************************************************

    '*********************************Annotation Tablet******************************
    Private AnnotationSelect As RadioButton = rbtnWhiteBoard

    Private Sub AnnotationTabletButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles rbtnWhiteBoard.Click, _
                                                                                                                  rbtnAnnotationAuxAVInput.Click, _
                                                                                                                  rbtnAnnotationVCPrimary.Click, _
                                                                                                                  rbtnAnnotationVCSecondary.Click, _
                                                                                                                  rbtnAnnotationDocumentCamera.Click, _
                                                                                                                  rbtnAnnotationGuestDevices.Click, _
                                                                                                                  rbtnAnnotationLecternComputer.Click, _
                                                                                                                  rbtnAnnotationPresenterCamera.Click '把信号接到视频采集卡
        Dim ATB As RadioButton = sender

        Select Case ATB.Name

            Case rbtnWhiteBoard.Name

            Case rbtnAnnotationAuxAVInput.Name
                PublicModule.Sources(5).VedioSwitch(Source.VedioOuput.AnnotationTabletVideoCollector)
            Case rbtnAnnotationVCPrimary.Name
                PublicModule.Sources(6).VedioSwitch(Source.VedioOuput.AnnotationTabletVideoCollector)
            Case rbtnAnnotationVCSecondary.Name
                PublicModule.Sources(7).VedioSwitch(Source.VedioOuput.AnnotationTabletVideoCollector)
            Case rbtnAnnotationDocumentCamera.Name
                PublicModule.Sources(2).VedioSwitch(Source.VedioOuput.AnnotationTabletVideoCollector)
            Case rbtnAnnotationGuestDevices.Name
                PublicModule.Sources(1).VedioSwitch(Source.VedioOuput.AnnotationTabletVideoCollector)
            Case rbtnAnnotationLecternComputer.Name
                PublicModule.Sources(0).VedioSwitch(Source.VedioOuput.AnnotationTabletVideoCollector)
            Case rbtnAnnotationPresenterCamera.Name
                PublicModule.Sources(4).VedioSwitch(Source.VedioOuput.AnnotationTabletVideoCollector)

        End Select
        AnnotationSelect = ATB

        'ExitNoTouchTimerReset()
    End Sub

    Private Sub WhiteBoardHttpSend()

        While True

            Try
                Dim httpUrl As System.Uri
                Select Case AnnotationSelect.Name

                    Case rbtnWhiteBoard.Name
                        httpUrl = New System.Uri("http://" & PublicModule.AllDevicesIPEndPoint(PublicModule.DevicesName.WhiteBoard).Address.ToString & ":" & PublicModule.AllDevicesIPEndPoint(PublicModule.DevicesName.WhiteBoard).Port.ToString & "/CMD=Signal_0")
                    Case Else
                        httpUrl = New System.Uri("http://" & PublicModule.AllDevicesIPEndPoint(PublicModule.DevicesName.WhiteBoard).Address.ToString & ":" & PublicModule.AllDevicesIPEndPoint(PublicModule.DevicesName.WhiteBoard).Port.ToString & "/CMD=Signal_1")
                End Select

                Dim req As System.Net.HttpWebRequest
                req = CType(System.Net.WebRequest.Create(httpUrl), System.Net.HttpWebRequest)
                req.Timeout = 5000
                req.Proxy = Nothing
                req.Method = "GET"

                Dim res As System.Net.HttpWebResponse = CType(req.GetResponse(), System.Net.HttpWebResponse)
                PublicModule.DebugMessageFileWrite("WhiteBoard(Send)：" & httpUrl.OriginalString)
                Dim reader As System.IO.StreamReader = New System.IO.StreamReader(res.GetResponseStream, System.Text.Encoding.ASCII)
                PublicModule.DebugMessageFileWrite("WhiteBoardCamera(Receive)：" & reader.ReadToEnd)

            Catch ex As Exception

            End Try

            Threading.Thread.Sleep(3000)
        End While

    End Sub

    Private Sub WhiteBoardHttpSend(ByVal S As String)
        Try
            Dim httpUrl As System.Uri

            httpUrl = New System.Uri("http://" & PublicModule.AllDevicesIPEndPoint(PublicModule.DevicesName.WhiteBoard).Address.ToString & ":" & PublicModule.AllDevicesIPEndPoint(PublicModule.DevicesName.WhiteBoard).Port.ToString & "/CMD=" & S)

            Dim req As System.Net.HttpWebRequest
            req = CType(System.Net.WebRequest.Create(httpUrl), System.Net.HttpWebRequest)
            req.Timeout = 5000
            req.Proxy = Nothing
            req.Method = "GET"

            Dim res As System.Net.HttpWebResponse = CType(req.GetResponse(), System.Net.HttpWebResponse)
            PublicModule.DebugMessageFileWrite("WhiteBoard(Send)：" & httpUrl.OriginalString)
            Dim reader As System.IO.StreamReader = New System.IO.StreamReader(res.GetResponseStream, System.Text.Encoding.ASCII)
            PublicModule.DebugMessageFileWrite("WhiteBoardCamera(Receive)：" & reader.ReadToEnd)

        Catch ex As Exception

        End Try
    End Sub

    '*********************************************************************************

    '*****************************DocumentCamera**************************************

    Private Sub DocumentCameraButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDocumentCameraZoomIn.Click, _
                                                                                                               btnDocumentCameraZoomOut.Click
        '控制按钮被点击
        Dim DCB As Button = sender

        Select Case DCB.Name
            Case "btnDocumentCameraUp"
                DocumentCam.SendCommand(DocumentCamera.Action.ARROW_Up)
            Case "btnDocumentCameraDown"
                DocumentCam.SendCommand(DocumentCamera.Action.ARROW_Down)
            Case "btnDocumentCameraLeft"
                DocumentCam.SendCommand(DocumentCamera.Action.ARROW_Left)
            Case "btnDocumentCameraRight"
                DocumentCam.SendCommand(DocumentCamera.Action.ARROW_Right)
            Case btnDocumentCameraZoomIn.Name
                DocumentCam.SendCommand(DocumentCamera.Action.ZoomIn)
            Case btnDocumentCameraZoomOut.Name
                DocumentCam.SendCommand(DocumentCamera.Action.ZoomOut)
            Case "btnDocumentCameraZoomReset"
                DocumentCam.SendCommand(DocumentCamera.Action.ZoomReset)
            Case "btnDocumentCameraAutoFocus"
                DocumentCam.SendCommand(DocumentCamera.Action.AUTO_FOCUS)
            Case "btnDocumentCameraRotate180"
                DocumentCam.SendCommand(DocumentCamera.Action.ROTATE_180)
            Case "btnDocumentCameraRotate270"
                DocumentCam.SendCommand(DocumentCamera.Action.ROTATE_270)
        End Select

        'ExitNoTouchTimerReset()
    End Sub

    Private Sub DocumentCameraRadioButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles rbtnDocumentCameraFreeze.Click, _
                                                                                                     rbtnDocumentCameraLandscape.Click, _
                                                                                                     rbtnDocumentCameraLightOFF.Click, _
                                                                                                     rbtnDocumentCameraLightUpper.Click, _
                                                                                                     rbtnDocumentCameraPortrait.Click, _
                                                                                                     rbtnDocumentCameraRelease.Click

        '控制RadioButton被点击
        Dim DCRB As RadioButton = sender

        Select Case DCRB.Name
            Case rbtnDocumentCameraLightUpper.Name  '灯光开

                DocumentCam.SendCommand(DocumentCamera.Action.LAMP_ON)
                rbtnDocumentCameraLightOFF.Enabled = True
                rbtnDocumentCameraLightUpper.Enabled = False


            Case rbtnDocumentCameraLightOFF.Name  '灯光

                DocumentCam.SendCommand(DocumentCamera.Action.LAMP_OFF)
                rbtnDocumentCameraLightOFF.Enabled = False
                rbtnDocumentCameraLightUpper.Enabled = True

            Case rbtnDocumentCameraFreeze.Name  '冻结
                'zxg
                ''DocumentCam_actionArray.Add(DocumentCamActionType.Freeze_DocumentCam)
                ''DocumentCam.SendCommand(DocumentCamera.Action.Freeze_Status)
                If DCRB.Checked <> DocumentCameraFreezeCheck Then
                    DocumentCam.SendCommand(DocumentCamera.Action.Freeze)
                    DocumentCameraFreezeCheck = True
                End If

            Case rbtnDocumentCameraRelease.Name  '解冻
                'zxg
                ''DocumentCam_actionArray.Add(DocumentCamActionType.UnFreeze_DocumentCam)
                ''DocumentCam.SendCommand(DocumentCamera.Action.Freeze_Status)
                If DCRB.Checked = DocumentCameraFreezeCheck Then
                    DocumentCam.SendCommand(DocumentCamera.Action.Freeze)
                    DocumentCameraFreezeCheck = False
                End If

            Case rbtnDocumentCameraPortrait.Name

                'DocumentCam.SendCommand(DocumentCamera.Action.Output_1024X768)
                DocumentCam.SendCommand(DocumentCamera.Action.ROTATE_90)

            Case rbtnDocumentCameraLandscape.Name

                DocumentCam.SendCommand(DocumentCamera.Action.ROTATE_0)

        End Select

        'ExitNoTouchTimerReset()
    End Sub

    Private Delegate Sub DocumentCameraReceivedCommandDelegate(ByVal RA As DocumentCamera.ReceivedAction) '收到DocumentCamera指令Delegate

    Private Sub DocumentCameraReceivedCommand(ByVal RA As DocumentCamera.ReceivedAction) Handles DocuemntCamEvents.ReceivedCommand '收到DocumentCamera指令
        Me.BeginInvoke(New DocumentCameraReceivedCommandDelegate(AddressOf DocumentCameraReceivedCommandInvoke), New Object() {RA})
    End Sub

    Private Sub DocumentCameraReceivedCommandInvoke(ByVal RA As DocumentCamera.ReceivedAction) '收到DocumentCamera指令Invoke

        Select Case RA

            Case DocumentCamera.ReceivedAction.Set_Freeze_On
                rbtnDocumentCameraFreeze.Checked = True
            Case DocumentCamera.ReceivedAction.Set_Freeze_Off
                rbtnDocumentCameraRelease.Checked = True
            Case DocumentCamera.ReceivedAction.Set_LAMP_On
                rbtnDocumentCameraLightUpper.Checked = True
            Case DocumentCamera.ReceivedAction.Set_LAMP_Off
                rbtnDocumentCameraLightOFF.Checked = True

        End Select

        'zxg
        Dim DocumentCam_action_tmp As DocumentCamActionType
        If DocumentCam_actionArray.Count > 0 Then
            DocumentCam_action_tmp = DocumentCam_actionArray(0)
            DocumentCam_actionArray.RemoveAt(0)
            Select Case RA

                Case DocumentCamera.ReceivedAction.Set_Freeze_On
                    If (DocumentCam_action_tmp = DocumentCamActionType.UnFreeze_DocumentCam) Then
                        DocumentCam.SendCommand(DocumentCamera.Action.Freeze)
                        rbtnDocumentCameraRelease.Checked = True
                    End If
                Case DocumentCamera.ReceivedAction.Set_Freeze_Off
                    If (DocumentCam_action_tmp = DocumentCamActionType.Freeze_DocumentCam) Then
                        DocumentCam.SendCommand(DocumentCamera.Action.Freeze)
                        rbtnDocumentCameraFreeze.Checked = True
                    End If
            End Select
        End If
    End Sub

    'zxg
    Private Sub OpenTV()
        Tv_action = TVActionType.OpenTV
        Tv_actionArray.Add(TVActionType.OpenTV)
        Tv.SendCommand(Tv.Action.CHECK)

        isTvChecking = True
        NoResponse_TV_Timer = New Threading.Thread(AddressOf NoResponse_TV)
        NoResponse_TV_Timer.Start(3000)
    End Sub
    Private Sub CloseTV()
        Tv_action = TVActionType.CloseTV
        Tv_actionArray.Add(TVActionType.CloseTV)
        Tv.SendCommand(Tv.Action.CHECK)

        isTvChecking = True
        NoResponse_TV_Timer = New Threading.Thread(AddressOf NoResponse_TV)
        NoResponse_TV_Timer.Start(3000)
    End Sub
    'zxg
    Private Delegate Sub TVReceivedCommandDelegate(ByVal RA As TV.ReceivedAction) '收到TV指令Delegate

    Private Sub TVReceivedCommand(ByVal RA As TV.ReceivedAction) Handles TVEvents.ReceivedCommand '收到TV指令
        Me.BeginInvoke(New TVReceivedCommandDelegate(AddressOf TVReceivedCommandInvoke), New Object() {RA})
    End Sub
    'zxg
    Private Sub TVReceivedCommandInvoke(ByVal RA As TV.ReceivedAction) '收到TV指令Invoke

        isTvChecking = False

        Dim Tv_action_tmp As TVActionType
        If Tv_actionArray.Count > 0 Then
            Tv_action_tmp = Tv_actionArray(0)
            Tv_actionArray.RemoveAt(0)
        Else
            'Tv_action_tmp = Tv_action
            Return
        End If

        Select Case RA

            Case Tv.ReceivedAction.Get_On
                Select Case Tv_action_tmp

                    Case TVActionType.CheckTV
                        ' Tv.SendCommand(Tv.Action.POWEROFF)
                        ' Tv.SendCommand(Tv.Action.POWERON) ', 2000)
                        Tv.SendCommand(Tv.Action.WAKEUP)
                        isTvWakeup = True
                    Case TVActionType.OpenTV
                        'If (Not isTvWakeup) Then
                        'Tv.SendCommand(Tv.Action.WAKEUPSLEEP)
                        Tv.SendCommand(Tv.Action.WAKEUP)
                        'End If
                        Tv.SendCommand(Tv.Action.HDMI)
                        isTvWakeup = True
                    Case TVActionType.CloseTV
                        If (isTvWakeup) Then
                            'Tv.SendCommand(Tv.Action.WAKEUPSLEEP)
                            Tv.SendCommand(Tv.Action.SLEEP)
                        End If
                        isTvWakeup = False
                    Case TVActionType.ShutdownTV
                        ' If (isTvWakeup) Then
                        'Tv.SendCommand(Tv.Action.POWEROFF)
                        ' Else
                        'Tv.SendCommand(Tv.Action.WAKEUPSLEEP)
                        Tv.SendCommand(Tv.Action.WAKEUP)
                        Tv.SendCommand(Tv.Action.POWEROFF, 1000)
                        'End If
                        isTvWakeup = False
                End Select
            Case Tv.ReceivedAction.Get_Off
                Select Case Tv_action_tmp

                    Case TVActionType.CheckTV
                        isTvWakeup = False
                    Case TVActionType.OpenTV
                        Tv.SendCommand(Tv.Action.POWERON)
                        Tv.SendCommand(Tv.Action.WAKEUP)
                        Tv.SendCommand(Tv.Action.HDMI) ', 2000)
                        isTvWakeup = True
                    Case TVActionType.CloseTV
                        isTvWakeup = False
                    Case TVActionType.ShutdownTV
                        isTvWakeup = False
                End Select
        End Select
    End Sub


    '**********************************************************************************

    '********************************PresenterCamera***********************************

    Private Sub PresenterCameraButton_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnPresenterCameraUp.Click, _
                                                                                                        btnPresenterCameraDown.Click, _
                                                                                                        btnPresenterCameraLeft.Click, _
                                                                                                        btnPresenterCameraRight.Click, _
                                                                                                        btnPresenterCameraZoomWide.Click, _
                                                                                                        btnPresenterCameraZoomTele.Click
        Dim PCBC As Button = sender
        Select Case PCBC.Name
            Case btnPresenterCameraUp.Name
                'PresenterCam.SendCommend(PresentCamera.Action.TileUp)
            Case btnPresenterCameraDown.Name
                'PresenterCam.SendCommend(PresentCamera.Action.TileDown)
            Case btnPresenterCameraLeft.Name
                'PresenterCam.SendCommend(PresentCamera.Action.PanLeft)
            Case btnPresenterCameraRight.Name
                'PresenterCam.SendCommend(PresentCamera.Action.PanRight)
            Case btnPresenterCameraZoomWide.Name
                'PresenterCam.SendCommend(PresentCamera.Action.ZoomWide)
            Case btnPresenterCameraZoomTele.Name
                'PresenterCam.SendCommend(PresentCamera.Action.ZoomTele)
        End Select

        'ExitNoTouchTimerReset()
    End Sub

    Private Sub PresenterCameraSkinChange_MouseUp(ByVal sender As Object, ByVal e As EventArgs) Handles btnPresenterCameraUp.MouseUp, _
                                                                                                   btnPresenterCameraDown.MouseUp, _
                                                                                                   btnPresenterCameraLeft.MouseUp, _
                                                                                                   btnPresenterCameraRight.MouseUp, _
                                                                                                   btnPresenterCameraZoomWide.MouseUp, _
                                                                                                   btnPresenterCameraZoomTele.MouseUp 'PresenterCamera面板按钮松开
        Dim PCCSC As Button = sender
        Select Case PCCSC.Name
            Case btnPresenterCameraUp.Name
                PCCSC.BackgroundImage = My.Resources.NewSkin.ArrowUp_Black
                PresenterCam.SendHttpCommand(PresentCamera.Action.Stop_Tile)
            Case btnPresenterCameraDown.Name
                PCCSC.BackgroundImage = My.Resources.NewSkin.ArrowDown_Black
                PresenterCam.SendHttpCommand(PresentCamera.Action.Stop_Tile)
            Case btnPresenterCameraLeft.Name
                PCCSC.BackgroundImage = My.Resources.NewSkin.ArrowLeft_Black
                PresenterCam.SendHttpCommand(PresentCamera.Action.Stop_Pan)
            Case btnPresenterCameraRight.Name
                PCCSC.BackgroundImage = My.Resources.NewSkin.ArrowRight_Black
                PresenterCam.SendHttpCommand(PresentCamera.Action.Stop_Pan)
            Case Else
                PCCSC.BackgroundImage = My.Resources.NewSkin.Background_White
                PresenterCam.SendHttpCommand(PresentCamera.Action.Stop_Zoom)
                'PCCSC.ForeColor = Color.White
        End Select

        'ExitNoTouchTimerReset()
    End Sub

    Private Sub PresenterCameraSkinChange_MouseDown(ByVal sender As Object, ByVal e As EventArgs) Handles btnPresenterCameraUp.MouseDown, _
                                                                                                   btnPresenterCameraDown.MouseDown, _
                                                                                                   btnPresenterCameraLeft.MouseDown, _
                                                                                                   btnPresenterCameraRight.MouseDown, _
                                                                                                   btnPresenterCameraZoomWide.MouseDown, _
                                                                                                   btnPresenterCameraZoomTele.MouseDown 'PresenterCamera面板按钮按下
        Dim PCCSC As Button = sender
        Select Case PCCSC.Name
            Case btnPresenterCameraUp.Name
                PCCSC.BackgroundImage = My.Resources.NewSkin.ArrowUp_BlackChecked
                PresenterCam.SendHttpCommand(PresentCamera.Action.TileUp)
            Case btnPresenterCameraDown.Name
                PCCSC.BackgroundImage = My.Resources.NewSkin.ArrowDown_BlackChecked
                PresenterCam.SendHttpCommand(PresentCamera.Action.TileDown)
            Case btnPresenterCameraLeft.Name
                PCCSC.BackgroundImage = My.Resources.NewSkin.ArrowLeft_BlackChecked
                PresenterCam.SendHttpCommand(PresentCamera.Action.PanLeft)
            Case btnPresenterCameraRight.Name
                PCCSC.BackgroundImage = My.Resources.NewSkin.ArrowRight_BlackChecked
                PresenterCam.SendHttpCommand(PresentCamera.Action.PanRight)
            Case btnPresenterCameraZoomTele.Name
                PCCSC.BackgroundImage = PublicModule.GetSkinImage(PublicModule.RoomButtonPressColor)
                PresenterCam.SendHttpCommand(PresentCamera.Action.ZoomTele)
            Case btnPresenterCameraZoomWide.Name
                PCCSC.BackgroundImage = PublicModule.GetSkinImage(PublicModule.RoomButtonPressColor)
                PresenterCam.SendHttpCommand(PresentCamera.Action.ZoomWide)
        End Select

        'ExitNoTouchTimerReset()
    End Sub

    '**********************************************************************************

    '*******************************ExamMode*******************************************

    Private ExamModeThread As Threading.Thread
    Private Const ExamModeInterval As Integer = 300000

    Private Sub chbExamMode_Click(ByVal sender As Object, ByVal e As EventArgs) Handles chbExamMode.Click '点击ExamMode
        Dim dialog As ModeDialog
        ExitNoTouchTimerReset()
        If chbExamMode.Checked Then
            dialog = New ModeDialog(ModeDialog.Action.BeginExamMode, False)
            If dialog.ShowDialog = Windows.Forms.DialogResult.Yes Then
                chbExamMode.Text = "Exam Mode" & vbCrLf & "(Active)"
                Exam_Mode = True
                'Dim screen As New Screen(screen.Action.ExamMode)
                'If screen.ShowDialog = Windows.Forms.DialogResult.Yes Then
                '添加ExamMode代码
                If PublicModule.LightProtocol <> 0 Then '灯光要调到Welcome
                    rbtnLightWelcome.Checked = True
                    rbtnLightPresets_Click(rbtnLightWelcome, New EventArgs)
                End If
                ExamModeThread = New Threading.Thread(AddressOf ExamModeKeepLight)
                ExamModeThread.Start()

                Try
                    ExitNoTouchTimer.Abort()
                Catch ex As Exception

                End Try

                'End If
            Else
                chbExamMode.Checked = False

            End If
        Else
            dialog = New ModeDialog(ModeDialog.Action.LeaveExamMode, False)
            If dialog.ShowDialog = Windows.Forms.DialogResult.Yes Then
                chbExamMode.Text = "Exam Mode"
                Exam_Mode = False
                Try
                    chbExamMode.Text = "Exam Mode"
                    Exam_Mode = False
                    ExamModeThread.Abort()
                    ExitNoTouchTimerReset()
                Catch ex As Exception

                End Try
            Else
                chbExamMode.Checked = True

            End If


        End If
    End Sub

    Private Sub ExamModeKeepLight()

        While Exam_Mode
            Threading.Thread.Sleep(ExamModeInterval)
            'If PublicModule.LightCon.CurrentLightMode <> LightControl.LightState.Welcome Then
            rbtnLightWelcome.Checked = True
            rbtnLightPresets_Click(rbtnLightWelcome, New EventArgs)
            'End If

        End While

    End Sub



    '**********************************************************************************

    '********************************Lighting******************************************

    Private Sub btnLightingBack_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnLightingBack.Click 'lighting面板退出
        If PreSourceSelect(0) = -1 And PreSourceSelect(1) = -1 Then
            RoomButtonUnCheck()
            tacFunction.Visible = False
        Else
            tacFunction.SelectedIndex = SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))
        End If
        lbCurrentFunctionTitle.Text = ""
        ProjectorLayoutVisable(True)
    End Sub

    Private Delegate Sub LightModeChangeDelegate(ByVal State As LightControl.LightState) '收到模式改变Delegate

    Private Sub LightModeChange(ByVal State As LightControl.LightState) Handles LightControlEvents.LightPresetsChange '收到模式改变
        Me.BeginInvoke(New LightModeChangeDelegate(AddressOf LightModeChangeInvoke), New Object() {State})
    End Sub

    Private Sub LightModeChangeInvoke(ByVal State As LightControl.LightState) '收到模式改变Invoke

        Try

            If State <> LightControl.LightState.Welcome And rbtnLightWelcome.Checked Then
                chbBoardLights1.Checked = False
                chbBoardLights2.Checked = False
                chbBoardLights3.Checked = False
            End If

            Select Case State

                Case LightControl.LightState.Welcome
                    rbtnLightWelcome.Checked = True
                    chbBoardLights1.Checked = True
                    chbBoardLights2.Checked = True
                    chbBoardLights3.Checked = True
                Case LightControl.LightState.Teach
                    rbtnLightTeach.Checked = True
                Case LightControl.LightState.Quality
                    rbtnLightQualityProjection.Checked = True
                Case LightControl.LightState.ExtraQuality
                    rbtnLightExtraQuality.Checked = True
                Case LightControl.LightState.BlackOut
                    rbtnLightBlackOut.Checked = True

            End Select

            PublicModule.LightCon.CurrentLightMode = State
        Catch ex As Exception
            'MessageBox.Show(ex.Message, "Lighting Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try


    End Sub

    Private Sub rbtnLightPresets_Click(ByVal sender As Object, ByVal e As EventArgs) Handles rbtnLightWelcome.Click, _
                                                                                            rbtnLightTeach.Click, _
                                                                                            rbtnLightQualityProjection.Click, _
                                                                                            rbtnLightExtraQuality.Click, _
                                                                                            rbtnLightBlackOut.Click
        Dim LPC As RadioButton = sender

        Try

            If PublicModule.LightCon.CurrentLightMode = LightControl.LightState.Welcome And Not rbtnLightWelcome.Checked Then
                chbBoardLights1.Checked = False
                chbBoardLights2.Checked = False
                chbBoardLights3.Checked = False
            End If

            Select Case LPC.Name

                Case rbtnLightWelcome.Name
                    chbBoardLights1.Checked = True
                    chbBoardLights2.Checked = True
                    chbBoardLights3.Checked = True
                    'PublicModule.LightCon.LightPresetChange(LightControl.LightState.Welcome)
                    LightControlhttp.SendHttpCommand(LightControlhttp.LightState.Welcome)
                Case rbtnLightTeach.Name
                    'PublicModule.LightCon.LightPresetChange(LightControl.LightState.Teach)
                    LightControlhttp.SendHttpCommand(LightControlhttp.LightState.Teach)
                Case rbtnLightQualityProjection.Name
                    'PublicModule.LightCon.LightPresetChange(LightControl.LightState.Quality)
                    LightControlhttp.SendHttpCommand(LightControlhttp.LightState.Quality)
                Case rbtnLightExtraQuality.Name
                    'PublicModule.LightCon.LightPresetChange(LightControl.LightState.ExtraQuality)
                    LightControlhttp.SendHttpCommand(LightControlhttp.LightState.ExtraQuality)
                Case rbtnLightBlackOut.Name
                    'PublicModule.LightCon.LightPresetChange(LightControl.LightState.BlackOut)
                    LightControlhttp.SendHttpCommand(LightControlhttp.LightState.BlackOut)

            End Select

        Catch ex As Exception
            'MessageBox.Show(ex.Message, "Lighting Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try





        'ExitNoTouchTimerReset()
    End Sub

    Private Sub chbBroadLights_Click(ByVal sender As Object, ByVal e As EventArgs) Handles chbBoardLights1.Click, _
                                                                                           chbBoardLights2.Click, _
                                                                                           chbBoardLights3.Click
        Dim BLC As CheckBox = sender
        Select Case BLC.Name

            Case chbBoardLights1.Name

                If BLC.CheckState Then
                    PublicModule.LightCon.BoardLightChange(LightControl.BoardLights.BoardLight1, LightControl.BoardLightState.SetOn)
                Else
                    PublicModule.LightCon.BoardLightChange(LightControl.BoardLights.BoardLight1, LightControl.BoardLightState.SetOff)
                End If

            Case chbBoardLights2.Name

                If BLC.CheckState Then
                    PublicModule.LightCon.BoardLightChange(LightControl.BoardLights.BoardLight2, LightControl.BoardLightState.SetOn)
                Else
                    PublicModule.LightCon.BoardLightChange(LightControl.BoardLights.BoardLight2, LightControl.BoardLightState.SetOff)
                End If

            Case chbBoardLights3.Name

                If BLC.CheckState Then
                    PublicModule.LightCon.BoardLightChange(LightControl.BoardLights.BoardLight3, LightControl.BoardLightState.SetOn)
                Else
                    PublicModule.LightCon.BoardLightChange(LightControl.BoardLights.BoardLight3, LightControl.BoardLightState.SetOff)
                End If


        End Select

        'ExitNoTouchTimerReset()
    End Sub




    '**********************************************************************************

    '*******************************Screen and display*********************************

    Private Sub ProjectorScreenUpDown_Click(ByVal sender As Object, ByVal e As EventArgs) Handles rbtnScreen1Up.Click, _
                                                                                                  rbtnScreen2Up.Click, _
                                                                                                  rbtnScreen1Down.Click, _
                                                                                                  rbtnScreen2Down.Click
        Dim PSUD As RadioButton = sender

        If PSUD.Checked Then

            Select Case PSUD.Name

                Case rbtnScreen1Up.Name
                    DeviceCon.SendMessage("30 1" & vbCrLf, PublicModule.DevicesName.Projector1Screen)
                Case rbtnScreen1Down.Name
                    DeviceCon.SendMessage("33 1" & vbCrLf, PublicModule.DevicesName.Projector1Screen)
                Case rbtnScreen2Up.Name
                    DeviceCon.SendMessage("30 1" & vbCrLf, PublicModule.DevicesName.Projector2Screen)
                Case rbtnScreen2Down.Name
                    DeviceCon.SendMessage("33 1" & vbCrLf, PublicModule.DevicesName.Projector2Screen)
            End Select

        Else

            Select Case PSUD.Name

                Case rbtnScreen1Up.Name

                Case rbtnScreen1Down.Name

                Case rbtnScreen2Up.Name

                Case rbtnScreen2Down.Name

            End Select

        End If

    End Sub


    Private Sub ProjectorScreenStop_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnScreen1Stop.Click, _
                                                                                               btnScreen2Stop.Click
        Dim PSS As Button = sender

        Select Case PSS.Name

            Case btnScreen1Stop.Name
                DeviceCon.SendMessage("36 1" & vbCrLf, PublicModule.DevicesName.Projector1Screen)
                rbtnScreen1Up.Checked = False
                rbtnScreen1Down.Checked = False
            Case btnScreen2Stop.Name
                DeviceCon.SendMessage("36 1" & vbCrLf, PublicModule.DevicesName.Projector2Screen)
                rbtnScreen2Up.Checked = False
                rbtnScreen2Down.Checked = False
        End Select


    End Sub

    Private Sub ProjectorCoolDown(ByVal P As ProjectorTcpControl.Projector)
        Threading.Thread.Sleep(240000)
        If P = ProjectorTcpControl.Projector.Projector1 Then
            PublicModule.Projector1Control.ProjectorCommandSent(ProjectorTcpControl.Action.Get_Power_Status)
        Else
            PublicModule.Projector2Control.ProjectorCommandSent(ProjectorTcpControl.Action.Get_Power_Status)
        End If
    End Sub




    Private Sub chbProjectorPower_Click(ByVal sender As Object, ByVal e As EventArgs) Handles chbProjector1Power.Click, _
                                                                                              chbProjector2Power.Click  '关闭或开启投影仪
        Dim PPC As CheckBox = sender
        Dim dialog As New ModeDialog(ModeDialog.Action.TurnOffProjector, False)
        Dim SetCoolDownTime As Threading.Thread
        Select Case PPC.Name

            Case chbProjector1Power.Name
                If chbProjector1Power.Checked Then
                    If dialog.ShowDialog = Windows.Forms.DialogResult.Yes Then
                        PublicModule.Projector1Control.ProjectorCommandSent(ProjectorTcpControl.Action.Power_Standby)
                        PublicModule.Projector1Control.ProjectorCommandSent(ProjectorTcpControl.Action.Get_Power_Status)
                        SetCoolDownTime = New Threading.Thread(AddressOf ProjectorCoolDown)
                        SetCoolDownTime.Start(ProjectorTcpControl.Projector.Projector1)
                    Else
                        PPC.Checked = False
                    End If
                Else
                    PublicModule.Projector1Control.ProjectorCommandSent(ProjectorTcpControl.Action.Power_On)

                End If

            Case chbProjector2Power.Name

                If chbProjector2Power.Checked Then
                    If dialog.ShowDialog = Windows.Forms.DialogResult.Yes Then
                        PublicModule.Projector2Control.ProjectorCommandSent(ProjectorTcpControl.Action.Power_Standby)
                        PublicModule.Projector2Control.ProjectorCommandSent(ProjectorTcpControl.Action.Get_Power_Status)
                        SetCoolDownTime = New Threading.Thread(AddressOf ProjectorCoolDown)
                        SetCoolDownTime.Start(ProjectorTcpControl.Projector.Projector2)
                    Else
                        PPC.Checked = False
                    End If
                Else
                    PublicModule.Projector2Control.ProjectorCommandSent(ProjectorTcpControl.Action.Power_On)

                End If

        End Select
    End Sub


    '**********************************************************************************

    '*******************************RoomSetting功能************************************
    Private Sub btnRoomSetting_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnRoomSetting.Click '按RoomSetting按钮
        Dim pd As New PasswordDialog(PasswordDialog.Action.RoomSetting) '检查密码
        If pd.ShowDialog = Windows.Forms.DialogResult.No Then
            Exit Sub
        End If



        RoomSettingLayoutVisable(True) '显示面板
        btnRoomSetting.Visible = False '按钮隐藏

        '读取Config数据
        tbxRoomSettingAudioDSPIP.Text = PublicModule.AllDevicesIPEndPoint(0).Address.ToString
        tbxRoomSettingAudioDSPPort.Text = PublicModule.AllDevicesIPEndPoint(0).Port.ToString

        tbxRoomSettingVideoSwitcherIP.Text = PublicModule.AllDevicesIPEndPoint(1).Address.ToString
        tbxRoomSettingVideoSwitcherPort.Text = PublicModule.AllDevicesIPEndPoint(1).Port.ToString

        tbxRoomSettingProjector1IP.Text = PublicModule.AllDevicesIPEndPoint(2).Address.ToString
        tbxRoomSettingProjector1Port.Text = PublicModule.AllDevicesIPEndPoint(2).Port.ToString

        tbxRoomSettingProjector2IP.Text = PublicModule.AllDevicesIPEndPoint(3).Address.ToString
        tbxRoomSettingProjector2Port.Text = PublicModule.AllDevicesIPEndPoint(3).Port.ToString

        tbxRoomSettingWhiteBoard.Text = PublicModule.WhileBoardPath

        cbxRoomSettingDocumentCameraCOM.Text = PublicModule.DocumentCameraCOM
        cbxRoomSettingDocumentCameraBuadrate.Text = PublicModule.DocumentCameraBuadrate

        cbxRoomSettingInfraredEquipmentCOM.Text = PublicModule.InfraredEquipmentCOM
        cbxRoomSettingInfraredEquipmentBuadrate.Text = PublicModule.InfraredEquipmentBuadrate

        tbxRoomSettingBackgroundImagePath.Text = PublicModule.BackGroundImagePath

        pbRoomSettingBaclgroundColor.BackColor = PublicModule.BackGroundColor

        ltvRoomSettingCommonButtonStyle.Items(PublicModule.CommonButtonColor).Selected = True

        ltvRoomSettingSourcesButtonPressStyle.Items(PublicModule.SourceAndProjectorButtonPressColor).Selected = True

        ltvRoomSettingRoomButtonPressStyle.Items(PublicModule.RoomButtonPressColor).Selected = True

        lbFontSelectTest.Font = PublicModule.AllButtonFont

        tbxRoomSettingPassword.Text = PublicModule.AdministratorPassword

        dtpRoomSettingExamModeExitTime.Value = PublicModule.ExamModeExitTime

        tbxRoomSettingNoOneTouchExitTime.Text = PublicModule.NoOneTouchExitTime

        For i As Integer = 0 To PublicModule.MicUse.GetUpperBound(0)
            ltvRoomSettingMicUse.Items(i).Checked = PublicModule.MicUse(i)
        Next

        cbxRoomSettingBoardLightUse.Text = PublicModule.BoardLight

        cbxRoomSettingPresenterSpotsUse.Text = PublicModule.PresenterSpots

        cbxRoomSettingAuxiluarySpotsUse.Text = PublicModule.AuxiluarySpots

        cbxRoomSettingOverFlowRoomUse.Text = PublicModule.GetRoomNameFromID(PublicModule.ThisOverFlowRoomIDSave)

        tbxRoomSettingOverFlowServerIP.Text = PublicModule.AllDevicesIPEndPoint(4).Address.ToString
        tbxRoomSettingOverFlowServerPort.Text = PublicModule.AllDevicesIPEndPoint(4).Port.ToString

        tbxRoomSettingLightProjectName.Text = PublicModule.LightProjectName
        tbxRoomSettingLightControlIP.Text = PublicModule.AllDevicesIPEndPoint(5).Address.ToString
        tbxRoomSettingLightControlPort.Text = PublicModule.AllDevicesIPEndPoint(5).Port.ToString
        tbxRoomSettingLightFeedbackIP.Text = PublicModule.AllDevicesIPEndPoint(6).Address.ToString
        tbxRoomSettingLightFeedbackPort.Text = PublicModule.AllDevicesIPEndPoint(6).Port.ToString
        cbxLightProtocol.SelectedIndex = PublicModule.LightProtocol
        chbLightPresetsBlackOutUse.Checked = PublicModule.LightPresetsBlackOutUse

        tbxRoomSettingPresenterCameraIP.Text = PublicModule.AllDevicesIPEndPoint(7).Address.ToString
        tbxRoomSettingPresenterCameraPort.Text = PublicModule.AllDevicesIPEndPoint(7).Port.ToString

        tbxRoomSettingProjector1ScreenIP.Text = PublicModule.AllDevicesIPEndPoint(8).Address.ToString
        tbxRoomSettingProjector1ScreenPort.Text = PublicModule.AllDevicesIPEndPoint(8).Port.ToString

        tbxRoomSettingProjector2ScreenIP.Text = PublicModule.AllDevicesIPEndPoint(9).Address.ToString
        tbxRoomSettingProjector2ScreenPort.Text = PublicModule.AllDevicesIPEndPoint(9).Port.ToString

        chbRoomSettingOverflowUse.Checked = PublicModule.OverFlowUse

        chbRoomSettingPresenterCameraUse.Checked = PublicModule.PresenterCameraUse

        tbxRoomSettingWhiteBoardIP.Text = PublicModule.AllDevicesIPEndPoint(10).Address.ToString
        tbxRoomSettingWhiteBoardPort.Text = PublicModule.AllDevicesIPEndPoint(10).Port.ToString

        chbRoomSettingScreenUse.Checked = PublicModule.ScreenUse

        dtpRoomSettingBlackOutTime.Value = PublicModule.BlackOutTime

    End Sub

    Private Sub tbxRoomSettingWhiteBoard_Click(ByVal sender As Object, ByVal e As EventArgs) Handles tbxRoomSettingWhiteBoard.Click '白板程序位置选择
        If ofdWhiteBoardAppPath.ShowDialog = Windows.Forms.DialogResult.OK Then
            tbxRoomSettingWhiteBoard.Text = ofdWhiteBoardAppPath.FileName
        End If
    End Sub

    Private Sub tbxRoomSettingBackgroundImagePath_Click(ByVal sender As Object, ByVal e As EventArgs) Handles tbxRoomSettingBackgroundImagePath.Click '选择背景图
        If ofdBackgroundImage.ShowDialog = Windows.Forms.DialogResult.OK Then
            tbxRoomSettingBackgroundImagePath.Text = ofdBackgroundImage.FileName
        End If
    End Sub

    Private Sub pbRoomSettingBaclgroundColor_Click(ByVal sender As Object, ByVal e As EventArgs) Handles pbRoomSettingBaclgroundColor.Click '选择背景颜色
        BackgroundColor.Color = pbRoomSettingBaclgroundColor.BackColor
        If BackgroundColor.ShowDialog = Windows.Forms.DialogResult.OK Then
            pbRoomSettingBaclgroundColor.BackColor = BackgroundColor.Color
        End If
    End Sub

    Private Sub lbFontSelectTest_Click(ByVal sender As Object, ByVal e As EventArgs) Handles lbFontSelectTest.Click '选择字体
        AllFontDialog.Font = PublicModule.AllButtonFont
        If AllFontDialog.ShowDialog = Windows.Forms.DialogResult.OK Then
            lbFontSelectTest.Font = AllFontDialog.Font
        End If
    End Sub

    Private Sub tbxRoomSettingPassword_Click(ByVal sender As Object, ByVal e As EventArgs) Handles tbxRoomSettingPassword.Click '输入新密码
        Dim pd As New PasswordDialog(PasswordDialog.Action.NewPassword)
        If pd.ShowDialog = Windows.Forms.DialogResult.Yes Then
            tbxRoomSettingPassword.Text = pd.GetNewPassword
        End If
    End Sub

    Private Sub chbEventMode_Click(ByVal sender As Object, ByVal e As EventArgs) Handles chbEventMode.Click  'EventMode
        Dim dialog As ModeDialog
        If chbEventMode.Checked Then
            dialog = New ModeDialog(ModeDialog.Action.BeginEventMode, False)
            If dialog.ShowDialog = Windows.Forms.DialogResult.Yes Then
                chbEventMode.Text = "Event Mode" & vbCrLf & "(Active)"
                Event_Mode = True

                Try
                    ExitNoTouchTimer.Abort() '关闭Timeout
                Catch ex As Exception

                End Try

                For Each s As Source In PublicModule.Sources '除了AUX外其它都。。。
                    If s.SourceID = 5 Then
                        s.SetVolumeMuted(Source.AudioDSPMute.UnMuted)
                        s.SetPreviewMute(Source.AudioDSPMute.Muted)
                    Else
                        s.SetVolumeMuted(Source.AudioDSPMute.Muted)
                        s.SetPreviewMute(Source.AudioDSPMute.Muted)
                    End If
                Next

                For a As Integer = 0 To PublicModule.MicUse.GetUpperBound(0) '根据配置要求,不使用的麦克风不予静音状态
                    If PublicModule.MicUse(a) Then
                        Select Case a

                            Case 0
                                chbWirelessMIC01.Checked = True
                                chbWirelessMIC01.CheckState = CheckState.Checked
                                PublicModule.RoomClasses(1).SetMicroPhoneMuted(Room.MicroPhoneMutePoint.WireLessMic01, Room.AudioDSPMute.Muted)
                            Case 1
                                chbWirelessMIC02.Checked = True
                                chbWirelessMIC02.CheckState = CheckState.Checked
                                PublicModule.RoomClasses(1).SetMicroPhoneMuted(Room.MicroPhoneMutePoint.WireLessMic02, Room.AudioDSPMute.Muted)
                            Case 2
                                chbWirelessMIC03.Checked = True
                                chbWirelessMIC03.CheckState = CheckState.Checked
                                PublicModule.RoomClasses(1).SetMicroPhoneMuted(Room.MicroPhoneMutePoint.WireLessMic03, Room.AudioDSPMute.Muted)
                            Case 3
                                chbWirelessMIC04.Checked = True
                                chbWirelessMIC04.CheckState = CheckState.Checked
                                PublicModule.RoomClasses(1).SetMicroPhoneMuted(Room.MicroPhoneMutePoint.WireLessMic04, Room.AudioDSPMute.Muted)

                        End Select
                    End If
                Next

                PublicModule.RoomClasses(1).SetMicroPhoneMuted(Room.MicroPhoneMutePoint.LecternMic, Room.AudioDSPMute.Muted)

                If PreSourceSelect(0) <> -1 And PreSourceSelect(1) <> -1 Then

                    chbLecternMuted.Checked = PublicModule.Sources(SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))).PreviewMuted
                    chbVolumeMuted.Checked = PublicModule.Sources(SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))).SourceMuted

                    If SourceIDPage(PreSourceSelect(0), PreSourceSelect(1)) = 0 Or SourceIDPage(PreSourceSelect(0), PreSourceSelect(1)) = 1 Then 'Lectern Guest的按键要失去作用
                        chbLecternMuted.Enabled = False
                        chbVolumeMuted.Enabled = False
                    End If

                End If

                For Each r As Room In PublicModule.RoomClasses
                    r.SetVolumeMuted(True)
                Next

                PublicModule.RoomClasses(4).PreviewMuted = True
                PublicModule.RoomClasses(4).SetVolumeMuted(Room.AudioDSPMute.Muted)

                btnRoomSetting.BackgroundImage = PublicModule.GetSkinImage(PublicModule.RoomButtonPressColor)
                btnRoomSetting.ForeColor = Color.Black
                btnRoomSettingBack_Click(btnRoomSettingBack, New EventArgs) '退出RoomSetting

                rbtnMicroPhones.Visible = False '隐藏MIC

            Else '选择No
                chbEventMode.Text = "Event Mode"
                chbEventMode.Checked = False
                chbEventMode.CheckState = CheckState.Unchecked
            End If

        Else '退出EventMode
            dialog = New ModeDialog(ModeDialog.Action.LeaveEventMode, False)
            If dialog.ShowDialog = Windows.Forms.DialogResult.Yes Then
                chbEventMode.Text = "Event Mode"
                chbEventMode.Checked = False
                chbEventMode.CheckState = CheckState.Unchecked
                Event_Mode = False
                'ExitNoTouchTimer.Enabled = True  '恢复Timeout
                ExitNoTouchTimerReset()

                For Each s As Source In PublicModule.Sources '除了AUX外其它都。。。
                    s.SetVolumeMuted(Source.AudioDSPMute.Muted)
                    s.SetPreviewMute(Source.AudioDSPMute.Muted)
                Next

                'For a As Integer = 0 To PublicModule.MicUse.GetUpperBound(0) '根据配置要求,不使用的麦克风不予恢复状态
                '    If PublicModule.MicUse(a) Then
                '        Select Case a

                '            Case 0
                '                chbWirelessMIC01.Checked = False
                '                chbWirelessMIC01.CheckState = CheckState.Unchecked
                '                PublicModule.RoomClasses(1).SetMicroPhoneMuted(Room.MicroPhoneMutePoint.WireLessMic01, Room.AudioDSPMute.UnMuted)
                '            Case 1
                '                chbWirelessMIC02.Checked = False
                '                chbWirelessMIC02.CheckState = CheckState.Unchecked
                '                PublicModule.RoomClasses(1).SetMicroPhoneMuted(Room.MicroPhoneMutePoint.WireLessMic02, Room.AudioDSPMute.UnMuted)
                '            Case 2
                '                chbWirelessMIC03.Checked = False
                '                chbWirelessMIC03.CheckState = CheckState.Unchecked
                '                PublicModule.RoomClasses(1).SetMicroPhoneMuted(Room.MicroPhoneMutePoint.WireLessMic03, Room.AudioDSPMute.UnMuted)
                '            Case 3
                '                chbWirelessMIC04.Checked = False
                '                chbWirelessMIC04.CheckState = CheckState.Unchecked
                '                PublicModule.RoomClasses(1).SetMicroPhoneMuted(Room.MicroPhoneMutePoint.WireLessMic04, Room.AudioDSPMute.UnMuted)

                '        End Select
                '    End If
                'Next

                'PublicModule.RoomClasses(1).SetMicroPhoneMuted(Room.MicroPhoneMutePoint.LecternMic, Room.AudioDSPMute.UnMuted)

                For a As Integer = 0 To PublicModule.MicUse.GetUpperBound(0) '根据配置要求,不使用的麦克风不予静音状态
                    If PublicModule.MicUse(a) Then
                        Select Case a

                            Case 0
                                chbWirelessMIC01.Checked = True
                                chbWirelessMIC01.CheckState = CheckState.Checked
                                PublicModule.RoomClasses(1).SetMicroPhoneMuted(Room.MicroPhoneMutePoint.WireLessMic01, Room.AudioDSPMute.Muted)
                            Case 1
                                chbWirelessMIC02.Checked = True
                                chbWirelessMIC02.CheckState = CheckState.Checked
                                PublicModule.RoomClasses(1).SetMicroPhoneMuted(Room.MicroPhoneMutePoint.WireLessMic02, Room.AudioDSPMute.Muted)
                            Case 2
                                chbWirelessMIC03.Checked = True
                                chbWirelessMIC03.CheckState = CheckState.Checked
                                PublicModule.RoomClasses(1).SetMicroPhoneMuted(Room.MicroPhoneMutePoint.WireLessMic03, Room.AudioDSPMute.Muted)
                            Case 3
                                chbWirelessMIC04.Checked = True
                                chbWirelessMIC04.CheckState = CheckState.Checked
                                PublicModule.RoomClasses(1).SetMicroPhoneMuted(Room.MicroPhoneMutePoint.WireLessMic04, Room.AudioDSPMute.Muted)

                        End Select
                    End If
                Next

                PublicModule.RoomClasses(1).SetMicroPhoneMuted(Room.MicroPhoneMutePoint.LecternMic, Room.AudioDSPMute.Muted)

                If PreSourceSelect(0) <> -1 And PreSourceSelect(1) <> -1 Then

                    chbLecternMuted.Checked = PublicModule.Sources(SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))).PreviewMuted
                    chbVolumeMuted.Checked = PublicModule.Sources(SourceIDPage(PreSourceSelect(0), PreSourceSelect(1))).SourceMuted

                    If SourceIDPage(PreSourceSelect(0), PreSourceSelect(1)) = 0 Or SourceIDPage(PreSourceSelect(0), PreSourceSelect(1)) = 1 Then 'Lectern Guest的按键要恢复作用
                        chbLecternMuted.Enabled = True
                        chbVolumeMuted.Enabled = True
                    End If

                End If

                For Each r As Room In PublicModule.RoomClasses
                    r.SetVolumeMuted(Room.AudioDSPMute.Muted)
                Next

                PublicModule.RoomClasses(4).PreviewMuted = True
                PublicModule.RoomClasses(4).SetVolumeMuted(Room.AudioDSPMute.Muted)

                btnRoomSetting.BackgroundImage = PublicModule.GetSkinImage(PublicModule.CommonButtonColor)
                btnRoomSetting.ForeColor = Color.White

                rbtnMicroPhones.Visible = True '显示MIC

            Else
                chbEventMode.Text = "Event Mode" & vbCrLf & "(Active)"
                chbEventMode.Checked = True
                chbEventMode.CheckState = CheckState.Checked
            End If
        End If

    End Sub

    Private Sub btnRoomSettingSave_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnRoomSettingSave.Click '保存Config

        '保存数据
        Try
            PublicModule.AllDevicesIPEndPoint(0) = New System.Net.IPEndPoint(System.Net.IPAddress.Parse(tbxRoomSettingAudioDSPIP.Text), Integer.Parse(tbxRoomSettingAudioDSPPort.Text))
            PublicModule.AllDevicesIPEndPoint(1) = New System.Net.IPEndPoint(System.Net.IPAddress.Parse(tbxRoomSettingVideoSwitcherIP.Text), Integer.Parse(tbxRoomSettingVideoSwitcherPort.Text))
            PublicModule.AllDevicesIPEndPoint(2) = New System.Net.IPEndPoint(System.Net.IPAddress.Parse(tbxRoomSettingProjector1IP.Text), Integer.Parse(tbxRoomSettingProjector1Port.Text))
            PublicModule.AllDevicesIPEndPoint(3) = New System.Net.IPEndPoint(System.Net.IPAddress.Parse(tbxRoomSettingProjector2IP.Text), Integer.Parse(tbxRoomSettingProjector2Port.Text))
            PublicModule.AllDevicesIPEndPoint(4) = New System.Net.IPEndPoint(System.Net.IPAddress.Parse(tbxRoomSettingOverFlowServerIP.Text), Integer.Parse(tbxRoomSettingOverFlowServerPort.Text))
            PublicModule.AllDevicesIPEndPoint(5) = New System.Net.IPEndPoint(System.Net.IPAddress.Parse(tbxRoomSettingLightControlIP.Text), Integer.Parse(tbxRoomSettingLightControlPort.Text))
            PublicModule.AllDevicesIPEndPoint(6) = New System.Net.IPEndPoint(System.Net.IPAddress.Parse(tbxRoomSettingLightFeedbackIP.Text), Integer.Parse(tbxRoomSettingLightFeedbackPort.Text))
            PublicModule.AllDevicesIPEndPoint(7) = New System.Net.IPEndPoint(System.Net.IPAddress.Parse(tbxRoomSettingPresenterCameraIP.Text), Integer.Parse(tbxRoomSettingPresenterCameraPort.Text))
            PublicModule.AllDevicesIPEndPoint(8) = New System.Net.IPEndPoint(System.Net.IPAddress.Parse(tbxRoomSettingProjector1ScreenIP.Text), Integer.Parse(tbxRoomSettingProjector1ScreenPort.Text))
            PublicModule.AllDevicesIPEndPoint(9) = New System.Net.IPEndPoint(System.Net.IPAddress.Parse(tbxRoomSettingProjector2ScreenIP.Text), Integer.Parse(tbxRoomSettingProjector2ScreenPort.Text))
            PublicModule.AllDevicesIPEndPoint(10) = New System.Net.IPEndPoint(System.Net.IPAddress.Parse(tbxRoomSettingWhiteBoardIP.Text), Integer.Parse(tbxRoomSettingWhiteBoardPort.Text))

            '插入重新连接代码

            PublicModule.WhileBoardPath = tbxRoomSettingWhiteBoard.Text

            PublicModule.DocumentCameraCOM = cbxRoomSettingDocumentCameraCOM.Text
            PublicModule.DocumentCameraBuadrate = Integer.Parse(cbxRoomSettingDocumentCameraBuadrate.Text)

            PublicModule.InfraredEquipmentCOM = cbxRoomSettingInfraredEquipmentCOM.Text
            PublicModule.InfraredEquipmentBuadrate = Integer.Parse(cbxRoomSettingInfraredEquipmentBuadrate.Text)

            '插入重新连接代码

            PublicModule.BackGroundImagePath = tbxRoomSettingBackgroundImagePath.Text
            PublicModule.BackGroundImage = Image.FromFile(tbxRoomSettingBackgroundImagePath.Text)

            PublicModule.BackGroundColor = pbRoomSettingBaclgroundColor.BackColor

            For i As Integer = 0 To ltvRoomSettingCommonButtonStyle.Items.Count - 1
                If ltvRoomSettingCommonButtonStyle.Items(i).Selected Then
                    PublicModule.CommonButtonColor = i
                End If
            Next

            For i As Integer = 0 To ltvRoomSettingSourcesButtonPressStyle.Items.Count - 1
                If ltvRoomSettingSourcesButtonPressStyle.Items(i).Selected Then
                    PublicModule.SourceAndProjectorButtonPressColor = i
                End If
            Next

            For i As Integer = 0 To ltvRoomSettingRoomButtonPressStyle.Items.Count - 1
                If ltvRoomSettingRoomButtonPressStyle.Items(i).Selected Then
                    PublicModule.RoomButtonPressColor = i
                End If
            Next

            PublicModule.AllButtonFont = lbFontSelectTest.Font

            PublicModule.AdministratorPassword = tbxRoomSettingPassword.Text

            PublicModule.ExamModeExitTime = dtpRoomSettingExamModeExitTime.Value

            PublicModule.NoOneTouchExitTime = Integer.Parse(tbxRoomSettingNoOneTouchExitTime.Text)

            For i As Integer = 0 To PublicModule.MicUse.GetUpperBound(0)
                PublicModule.MicUse(i) = ltvRoomSettingMicUse.Items(i).Checked
            Next

            PublicModule.BoardLight = Integer.Parse(cbxRoomSettingBoardLightUse.Text)

            PublicModule.PresenterSpots = Integer.Parse(cbxRoomSettingPresenterSpotsUse.Text)

            PublicModule.AuxiluarySpots = Integer.Parse(cbxRoomSettingAuxiluarySpotsUse.Text)

            'zxg
            'PublicModule.ThisOverFlowRoomIDSave = cbxRoomSettingOverFlowRoomUse.SelectedIndex
            PublicModule.ThisOverFlowRoomIDSave = cbxRoomSettingOverFlowRoomUse.SelectedIndex + 2

            PublicModule.LightProjectName = tbxRoomSettingLightProjectName.Text

            PublicModule.LightProtocol = cbxLightProtocol.SelectedIndex

            PublicModule.LightPresetsBlackOutUse = chbLightPresetsBlackOutUse.Checked

            PublicModule.OverFlowUse = chbRoomSettingOverflowUse.Checked

            PublicModule.PresenterCameraUse = chbRoomSettingPresenterCameraUse.Checked

            PublicModule.ScreenUse = chbRoomSettingScreenUse.Checked

            PublicModule.BlackOutTime = dtpRoomSettingBlackOutTime.Value

            Dim MAU As New MainAppearanceUpdate(Me) '界面更新
            MAU.UpdateAppearance()

            Dim SMC As New SaveXmlConfiguration(PublicModule.ConfigurationPath & "\Configuration.xml")

        Catch ex As Exception
            MessageBox.Show(ex.Message, "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

    End Sub

    Private Sub btnRoomSettingBack_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnRoomSettingBack.Click '返回按钮
        RoomSettingLayoutVisable(False)
        btnRoomSetting.Visible = True
        RoomSettingTextboxCursor = -1
        RoomSettingTextboxSelect = -1

        'zxg update receive disconnect room name here
        If (OverFlowReceiveMode) Then
            If (PublicModule.ThisOverFlowRoomID = 2) Then
                btnOverFlowEngagedDisconnect.Text = "Disconnect" & vbCrLf & "from" & vbCrLf & "Room J" & PublicModule.Overflowdisconnectfrom
            Else
                btnOverFlowEngagedDisconnect.Text = "Disconnect" & vbCrLf & "from" & vbCrLf & "Room J" & PublicModule.Overflowdisconnectfrom
            End If
            'If (PublicModule.ThisOverFlowRoomID = 2) Then
            '    btnOverFlowEngagedDisconnect.Text = "Disconnect" & vbCrLf & "from" & vbCrLf & PublicModule.GetRoomNameFromID(3)
            'Else
            '    btnOverFlowEngagedDisconnect.Text = "Disconnect" & vbCrLf & "from" & vbCrLf & PublicModule.GetRoomNameFromID(2)
            'End If
            ' btnOverFlowEngagedDisconnect.Text = "Disconnect" & vbCrLf & "from" & vbCrLf & PublicModule.GetRoomNameFromID(PublicModule.ThisOverFlowRoomID)
        End If


    End Sub

    Private Sub btnRoomSettingKeyPad_MouseDown(ByVal sender As Object, ByVal e As EventArgs) Handles btnRoomSettingKeyPad0.MouseDown, _
                                                                                                    btnRoomSettingKeyPad1.MouseDown, _
                                                                                                    btnRoomSettingKeyPad2.MouseDown, _
                                                                                                    btnRoomSettingKeyPad3.MouseDown, _
                                                                                                    btnRoomSettingKeyPad4.MouseDown, _
                                                                                                    btnRoomSettingKeyPad5.MouseDown, _
                                                                                                    btnRoomSettingKeyPad6.MouseDown, _
                                                                                                    btnRoomSettingKeyPad7.MouseDown, _
                                                                                                    btnRoomSettingKeyPad8.MouseDown, _
                                                                                                    btnRoomSettingKeyPad9.MouseDown, _
                                                                                                    btnRoomSettingKeyPadDot.MouseDown, _
                                                                                                    btnRoomSettingKeyPadBackSpace.MouseDown 'RoomSetting九宫格按键
        Dim RSKP As Button = sender
        RSKP.ForeColor = Color.Black
        RSKP.BackgroundImage = PublicModule.GetSkinImage(PublicModule.RoomButtonPressColor)
        If RoomSettingTextboxSelect <> -1 And Not RSKP.Equals(btnRoomSettingKeyPadBackSpace) Then
            RoomSettingTextbox(RoomSettingTextboxSelect).AppendText(RSKP.Text)
            RoomSettingTextbox(RoomSettingTextboxSelect).Focus()
        ElseIf RoomSettingTextboxSelect <> -1 And RSKP.Equals(btnRoomSettingKeyPadBackSpace) Then
            If RoomSettingTextbox(RoomSettingTextboxSelect).Text.Length > 0 Then
                RoomSettingTextbox(RoomSettingTextboxSelect).Text = RoomSettingTextbox(RoomSettingTextboxSelect).Text.Substring(0, RoomSettingTextbox(RoomSettingTextboxSelect).Text.Length - 1)
            End If
        End If
    End Sub

    Private Sub btnRoomSettingKeyPad_MouseUp(ByVal sender As Object, ByVal e As EventArgs) Handles btnRoomSettingKeyPad0.MouseUp, _
                                                                                                    btnRoomSettingKeyPad1.MouseUp, _
                                                                                                    btnRoomSettingKeyPad2.MouseUp, _
                                                                                                    btnRoomSettingKeyPad3.MouseUp, _
                                                                                                    btnRoomSettingKeyPad4.MouseUp, _
                                                                                                    btnRoomSettingKeyPad5.MouseUp, _
                                                                                                    btnRoomSettingKeyPad6.MouseUp, _
                                                                                                    btnRoomSettingKeyPad7.MouseUp, _
                                                                                                    btnRoomSettingKeyPad8.MouseUp, _
                                                                                                    btnRoomSettingKeyPad9.MouseUp, _
                                                                                                    btnRoomSettingKeyPadDot.MouseUp, _
                                                                                                    btnRoomSettingKeyPadBackSpace.MouseUp 'RoomSetting九宫格按键弹起
        Dim RSKP As Button = sender
        RSKP.ForeColor = Color.White
        RSKP.BackgroundImage = PublicModule.GetSkinImage(PublicModule.CommonButtonColor)


    End Sub

    Private Sub SaveTheFoucusTextbox(ByVal sender As Object, ByVal e As EventArgs)
        Dim SFT As TextBox = sender
        If SFT.Focused Then
            RoomSettingTextboxSelect = Array.IndexOf(RoomSettingTextbox, SFT)

        End If
    End Sub


    '**********************************************************************************

    '********************************OverFlow******************************************
    Private Delegate Sub StandAloneSetDelegate(ByVal RoomID As Integer) '委托
    Private Delegate Sub WaitingSetDelegate(ByVal Request_ID As Integer, ByVal Waiting_ID As Integer)
    Private Delegate Sub SendingSetDelegate(ByVal Sending_ID As Integer, ByVal Receiving_ID As Integer)
    Private Delegate Sub ReceivingSetDelegate(ByVal Sending_ID As Integer, ByVal Receiving_ID As Integer)
    Private Delegate Sub RequestSendSuccessDelegate(ByVal Waiting_ID As Integer)
    Private Delegate Sub AcceptReceiveSuccessDelegate(ByVal Sending_ID As Integer)
    Private Delegate Sub RequestReceiveSuccessDelegate(ByVal Sending_ID As Integer)
    Private Delegate Sub RequestReceiveDisconnectSuccessDelegate(ByVal Sending_ID As Integer)

    Private Sub StandAloneSet(ByVal RoomID As Integer) '设置某Room为StandAlone
        Me.BeginInvoke(New StandAloneSetDelegate(AddressOf StandAloneSetInvoke), New Object() {RoomID})
    End Sub

    Private Sub WaitingSet(ByVal Request_ID As Integer, ByVal Waiting_ID As Integer) '设置某Room为Waiting
        Me.BeginInvoke(New WaitingSetDelegate(AddressOf WaitingSetInvoke), New Object() {Request_ID, Waiting_ID})
    End Sub

    Private Sub SendingSet(ByVal Sending_ID As Integer, ByVal Receiving_ID As Integer) '设置某Room为Sending
        Me.BeginInvoke(New SendingSetDelegate(AddressOf SendingSetInvoke), New Object() {Sending_ID, Receiving_ID})
    End Sub

    Private Sub ReceivingSet(ByVal Sending_ID As Integer, ByVal Receiving_ID As Integer) '设置某Room为Receiving
        Me.BeginInvoke(New ReceivingSetDelegate(AddressOf ReceivingSetInvoke), New Object() {Sending_ID, Receiving_ID})
    End Sub

    Private Sub RequestSendSuccess(ByVal Waiting_ID As Integer) '成功发送请求
        Me.BeginInvoke(New RequestSendSuccessDelegate(AddressOf RequestSendSuccessInvoke), New Object() {Waiting_ID})
    End Sub

    Private Sub AcceptReceiveSuccess(ByVal Sending_ID As Integer) '这里确认接收成功
        Me.BeginInvoke(New AcceptReceiveSuccessDelegate(AddressOf AcceptReceiveSuccessInvoke), New Object() {Sending_ID})
    End Sub

    Private Sub RequestReceiveSuccess(ByVal Sending_ID As Integer) '共同接收发送成功
        Me.BeginInvoke(New RequestReceiveSuccessDelegate(AddressOf RequestReceiveSuccessInvoke), New Object() {Sending_ID})
    End Sub

    Private Sub RequestReceiveDisconnectSuccess(ByVal Sending_ID As Integer) '请求断开接收成功
        Me.BeginInvoke(New RequestReceiveDisconnectSuccessDelegate(AddressOf RequestReceiveDisconnectSuccessInvoke), New Object() {Sending_ID})
    End Sub

    Private Sub StandAloneSetInvoke(ByVal RoomID As Integer) '设置某Room为StandAlone Invoke
        Dim MyOFR As OverFlowRoom = PublicModule.GetOverFlowRoomFromID(PublicModule.ThisOverFlowRoomID)
        Dim OFR As OverFlowRoom = PublicModule.GetOverFlowRoomFromID(RoomID)
        If tlpRoomStates.Visible Or rbtnEndSession.Checked Or rbtnOverFlowStandAlone.Checked Then '在布局显示情况下更改按钮
            If RoomID <> PublicModule.ThisOverFlowRoomID Then
                If OFR.RoomState = OverFlowRoom.MyRoomState.Requesting Then
                    OverFlowRoomButtons(RoomID).Text = PublicModule.GetRoomNameFromID(RoomID) & vbCrLf & "(Sending)"
                    OverFlowRoomButtons(RoomID).ForeColor = Color.Black
                    OverFlowRoomButtons(RoomID).BackgroundImage = My.Resources.NewSkin.Background_Grey
                Else
                    OverFlowRoomButtons(RoomID).Text = PublicModule.GetRoomNameFromID(RoomID) & vbCrLf & "(Stand Alone)"
                    'OverFlowRoomButtons(RoomID).ForeColor = Color.White
                    OverFlowRoomButtons(RoomID).BackgroundImage = My.Resources.NewSkin.Background_White
                End If

            Else
                If MyOFR.Sending.Count = 0 Then  '接收方断开连接
                    '当这个房间还在发送状态，但发送队伍无人 退出发送状态
                    'zxg 
                    If (OverFlowSendMode) Then
                        DeviceCon.SendMessage("0*" & Source.VedioOuput.OverFlowPrimary & "!", DevicesName.VideoMatrixSwitcher)
                    End If


                    OverFlowSendMode = False
                    rbtnOverFlow.Text = "OverFlow"
                    If PreRoomSelect = 4 Then
                        rbtnOverFlow.ForeColor = Color.Black
                        rbtnOverFlow.BackgroundImage = PublicModule.GetSkinImage(PublicModule.RoomButtonPressColor)
                    Else
                        rbtnOverFlow.ForeColor = Color.White
                        rbtnOverFlow.BackgroundImage = PublicModule.GetSkinImage(PublicModule.CommonButtonColor)
                    End If

                    If rbtnOverFlowModeSingle.Checked Then
                        btnProjector2.Enabled = True '解开投影仪2按键
                    End If

                End If
            End If
        End If
    End Sub

    Private Sub WaitingSetInvoke(ByVal Request_ID As Integer, ByVal Waiting_ID As Integer) '设置某Room为Waiting Invoke
        Dim MyOFR As OverFlowRoom = PublicModule.GetOverFlowRoomFromID(PublicModule.ThisOverFlowRoomID)
        If tlpRoomStates.Visible Then
            If Waiting_ID = PublicModule.ThisOverFlowRoomID Then '自己房间时等待接收
                If tlpOverFlowMode.Visible Then 'Send面板
                    OverFlowRoomButtons(Request_ID).Text = PublicModule.GetRoomNameFromID(Request_ID) & vbCrLf & "(Sending)"
                    OverFlowRoomButtons(Request_ID).ForeColor = Color.Black
                    OverFlowRoomButtons(Request_ID).BackgroundImage = My.Resources.NewSkin.Background_Grey
                Else 'Receive面板
                    OverFlowRoomButtons(Request_ID).Text = PublicModule.GetRoomNameFromID(Request_ID) & vbCrLf & "(Sending)"
                    OverFlowRoomButtons(Request_ID).ForeColor = Color.Black
                    OverFlowRoomButtons(Request_ID).BackgroundImage = My.Resources.NewSkin.Background_Orange
                End If
            Else
                If MyOFR.Waiting.Count = 0 Then
                    OverFlowRoomButtons(Request_ID).Text = PublicModule.GetRoomNameFromID(Request_ID) & vbCrLf & "(Sending)"
                    OverFlowRoomButtons(Request_ID).ForeColor = Color.Black
                    OverFlowRoomButtons(Request_ID).BackgroundImage = My.Resources.NewSkin.Background_Grey
                End If
                OverFlowRoomButtons(Waiting_ID).Text = PublicModule.GetRoomNameFromID(Waiting_ID) & vbCr & "(Waiting)"
                OverFlowRoomButtons(Waiting_ID).ForeColor = Color.Black
                OverFlowRoomButtons(Waiting_ID).BackgroundImage = My.Resources.NewSkin.Background_Grey
            End If
        End If
    End Sub

    Private Sub SendingSetInvoke(ByVal Sending_ID As Integer, ByVal Receiving_ID As Integer) '设置某Room为Sending Invoke
        Dim MyOFR As OverFlowRoom = PublicModule.GetOverFlowRoomFromID(PublicModule.ThisOverFlowRoomID)
        If Sending_ID = PublicModule.ThisOverFlowRoomID Then '对方同意接收本房间
            If tlpOverFlowMode.Visible Then 'Send面板
                OverFlowRoomButtons(Receiving_ID).Text = PublicModule.GetRoomNameFromID(Receiving_ID) & vbCr & "(Receiving)"
                OverFlowRoomButtons(Receiving_ID).ForeColor = Color.Black
                OverFlowRoomButtons(Receiving_ID).BackgroundImage = My.Resources.NewSkin.Background_BrightRed
            Else 'Receive面板
                OverFlowRoomButtons(Receiving_ID).Text = PublicModule.GetRoomNameFromID(Receiving_ID) & vbCr & "(Receiving)"
                OverFlowRoomButtons(Receiving_ID).ForeColor = Color.Black
                OverFlowRoomButtons(Receiving_ID).BackgroundImage = My.Resources.NewSkin.Background_Grey
            End If

            If ProjectorUse(0) <> -1 And ProjectorUse(1) <> ProjectorUse(0) And rbtnOverFlowModeSingle.Checked Then '如果projector1 与 projector2 有输出,不一致,那么projector1 与proejctor2 一致
                Dim SelectSave() As Integer = PreSourceSelect

                For i As Integer = 0 To SourceIDPage.GetUpperBound(0)
                    For j As Integer = 0 To SourceIDPage.GetUpperBound(1)
                        If SourceIDPage(i, j) = ProjectorUse(0) Then
                            PreSourceSelect(0) = i
                            PreSourceSelect(1) = j
                        End If
                    Next
                Next

                btnProjector_Click(btnProjector2, New EventArgs)

                PreSourceSelect = SelectSave
            ElseIf ProjectorUse(0) = -1 And ProjectorUse(1) <> -1 And rbtnOverFlowModeSingle.Checked Then

                Dim SelectSave() As Integer = PreSourceSelect

                For i As Integer = 0 To SourceIDPage.GetUpperBound(0)
                    For j As Integer = 0 To SourceIDPage.GetUpperBound(1)
                        If SourceIDPage(i, j) = ProjectorUse(1) Then
                            PreSourceSelect(0) = i
                            PreSourceSelect(1) = j
                        End If
                    Next
                Next

                btnProjector_Click(btnProjector1, New EventArgs)

                PreSourceSelect = SelectSave

            End If

            OverFlowSendMode = True
            rbtnOverFlow.Text = "OverFlow" & vbCrLf & "(Sending)"
            rbtnOverFlow.ForeColor = Color.Black
            rbtnOverFlow.BackgroundImage = My.Resources.NewSkin.Background_BrightRed

            If rbtnOverFlowModeSingle.Checked Then
                If ProjectorUse(0) <> -1 Then
                    DeviceCon.SendMessage(Sources(ProjectorUse(0)).VedioInputMap(ProjectorUse(0)) & "*" & Source.VedioOuput.OverFlowPrimary & "!", DevicesName.VideoMatrixSwitcher)
                Else
                    DeviceCon.SendMessage("0*" & Source.VedioOuput.OverFlowPrimary & "!", PublicModule.DevicesName.VideoMatrixSwitcher)
                End If
                DeviceCon.SendMessage(Source.VedioInput.PTZRoomCamera & "*" & Source.VedioOuput.OverFlowSecondary & "!", PublicModule.DevicesName.VideoMatrixSwitcher)
            ElseIf rbtnOverFlowModeDouble.Checked Then
                If ProjectorUse(0) <> -1 Then
                    DeviceCon.SendMessage(Sources(ProjectorUse(0)).VedioInputMap(ProjectorUse(0)) & "*" & Source.VedioOuput.OverFlowPrimary & "!", DevicesName.VideoMatrixSwitcher)
                Else
                    DeviceCon.SendMessage("0*" & Source.VedioOuput.OverFlowPrimary & "!", PublicModule.DevicesName.VideoMatrixSwitcher)
                End If
                If ProjectorUse(1) <> -1 Then
                    DeviceCon.SendMessage(Sources(ProjectorUse(1)).VedioInputMap(ProjectorUse(1)) & "*" & Source.VedioOuput.OverFlowSecondary & "!", DevicesName.VideoMatrixSwitcher)
                Else
                    DeviceCon.SendMessage("0*" & Source.VedioOuput.OverFlowSecondary & "!", PublicModule.DevicesName.VideoMatrixSwitcher)
                End If
            End If

            If rbtnOverFlowModeSingle.Checked Then
                btnProjector2.Enabled = False '锁定投影仪2按键
            End If

        ElseIf Sending_ID <> MyOFR.ID And Receiving_ID = Nothing Then '单独设置Sending
            OverFlowRoomButtons(Sending_ID).Text = PublicModule.GetRoomNameFromID(Sending_ID) & vbCrLf & "(Sending)"
            OverFlowRoomButtons(Sending_ID).ForeColor = Color.Black
            OverFlowRoomButtons(Sending_ID).BackgroundImage = My.Resources.NewSkin.Background_Grey
        Else
            If Not MyOFR.Waiting.Contains(Sending_ID) And Sending_ID <> PublicModule.OverFlowRoomCount Then
                OverFlowRoomButtons(Sending_ID).Text = PublicModule.GetRoomNameFromID(Sending_ID) & vbCrLf & "(Sending)"
                OverFlowRoomButtons(Sending_ID).ForeColor = Color.Black
                OverFlowRoomButtons(Sending_ID).BackgroundImage = My.Resources.NewSkin.Background_Grey
            End If
            OverFlowRoomButtons(Receiving_ID).Text = PublicModule.GetRoomNameFromID(Receiving_ID) & vbCr & "(Receiving)"
            OverFlowRoomButtons(Receiving_ID).ForeColor = Color.Black
            OverFlowRoomButtons(Receiving_ID).BackgroundImage = My.Resources.NewSkin.Background_Grey
        End If

    End Sub

    Private Sub ReceivingSetInvoke(ByVal Sending_ID As Integer, ByVal Receiving_ID As Integer) '设置某Room为Receiving Invoke
        If tlpRoomStates.Visible Then
            If Sending_ID <> PublicModule.OverFlowRoomCount Then '不是ControlRoom时操作
                OverFlowRoomButtons(Sending_ID).Text = PublicModule.GetRoomNameFromID(Sending_ID) & vbCrLf & "(Sending)"
                OverFlowRoomButtons(Sending_ID).ForeColor = Color.Black
                OverFlowRoomButtons(Sending_ID).BackgroundImage = My.Resources.NewSkin.Background_Grey
            End If

            OverFlowRoomButtons(Receiving_ID).Text = PublicModule.GetRoomNameFromID(Receiving_ID) & vbCr & "(Receiving)"
            OverFlowRoomButtons(Receiving_ID).ForeColor = Color.Black
            OverFlowRoomButtons(Receiving_ID).BackgroundImage = My.Resources.NewSkin.Background_Grey
        End If
    End Sub

    Private Sub RequestSendSuccessInvoke(ByVal Waiting_ID As Integer) '成功发送请求 Invoke
        If tlpRoomStates.Visible Then
            OverFlowRoomButtons(Waiting_ID).Text = PublicModule.GetRoomNameFromID(Waiting_ID) & vbCrLf & "(Waiting)"
            OverFlowRoomButtons(Waiting_ID).ForeColor = Color.Black
            OverFlowRoomButtons(Waiting_ID).BackgroundImage = My.Resources.NewSkin.Background_Orange
        End If
    End Sub

    Private Sub AcceptReceiveSuccessInvoke(ByVal Sending_ID As Integer) '这里确认接收成功 Invoke
        RoomButton_Click(rbtnOverFlow, New EventArgs)
        OverFlowLayoutVisable(False)
        SourceLayoutVisable(False)
        rbtnMicroPhones.Visible = False
        chbExamMode.Visible = False
        VolumeLayoutVisable(True)
        ProjectorLayoutVisable(False)
        OverFlowReceiveMode = True

        rbtnOverFlow.Text = "OverFlow" & vbCrLf & "(Receiving)"
        rbtnOverFlow.ForeColor = Color.Black
        rbtnOverFlow.BackgroundImage = My.Resources.NewSkin.Background_BrightRed

        If Sending_ID <> PublicModule.OverFlowRoomCount Then '
            btnOverFlowEngagedDisconnect.Text = "Disconnect" & vbCrLf & "from" & vbCrLf & "Room J" & PublicModule.Overflowdisconnectfrom
            'btnOverFlowEngagedDisconnect.Text = "Disconnect" & vbCrLf & "from" & vbCrLf & PublicModule.GetRoomNameFromID(Sending_ID)
            OverFlowRoomButtons(Sending_ID).Text = PublicModule.GetRoomNameFromID(Sending_ID) & vbCrLf & "(Sending)"
            OverFlowRoomButtons(Sending_ID).ForeColor = Color.Black
            OverFlowRoomButtons(Sending_ID).BackgroundImage = My.Resources.NewSkin.Background_Grey
        Else 'ControlRoom
            btnOverFlowEngagedDisconnect.Text = "Disconnect" & vbCrLf & "from" & vbCrLf & "Conrtol Room"
        End If

        Dim PreSourceSelectSave() As Integer = PreSourceSelect
        Dim ProjectorUseSave() As Integer = ProjectorUse

        DeviceCon.SendMessage(Source.VedioInput.OverFlowPrimary & "*" & Source.VedioOuput.Projector1 & "!", DevicesName.VideoMatrixSwitcher)
        DeviceCon.SendMessage(Source.VedioInput.OverFlowPrimary & "*" & Source.VedioOuput.RecordingDigitizer & "!", DevicesName.VideoMatrixSwitcher)
        DeviceCon.SendMessage(Source.VedioInput.OverFlowSecondary & "*" & Source.VedioOuput.Projector2 & "!", DevicesName.VideoMatrixSwitcher)

        PublicModule.RoomClasses(4).SetVolumeMuted(Room.AudioDSPMute.UnMuted)
        PublicModule.RoomClasses(4).PreviewMuted = True

        If PreRoomSelect = 4 Then
            chbVolumeMuted.Checked = PublicModule.RoomClasses(4).VolumeMuted
            chbLecternMuted.Checked = PublicModule.RoomClasses(4).PreviewMuted
        End If


        DeviceCon.SendMessage(Source.VedioOuput.Projector1 & "*0B", DevicesName.VideoMatrixSwitcher)
        DeviceCon.SendMessage(Source.VedioOuput.RecordingDigitizer & "*0B", DevicesName.VideoMatrixSwitcher)
        PublicModule.Projector1Control.ProjectorCommandSent(ProjectorTcpControl.Action.Power_On)
        chbProjector1Power.Checked = False
        PublicModule.Projector1Control.ProjectorCommandSent(ProjectorTcpControl.Action.AV_Mute_Off)
        PublicModule.Projector1Control.ProjectorCommandSent(ProjectorTcpControl.Action.Input_HDMI)
        DeviceCon.SendMessage("33 1" & vbCrLf, PublicModule.DevicesName.Projector1Screen)

        DeviceCon.SendMessage(Source.VedioOuput.Projector2 & "*0B", DevicesName.VideoMatrixSwitcher)
        PublicModule.Projector2Control.ProjectorCommandSent(ProjectorTcpControl.Action.Power_On)
        chbProjector2Power.Checked = False
        PublicModule.Projector2Control.ProjectorCommandSent(ProjectorTcpControl.Action.AV_Mute_Off)
        PublicModule.Projector2Control.ProjectorCommandSent(ProjectorTcpControl.Action.Input_HDMI)
        DeviceCon.SendMessage("33 1" & vbCrLf, PublicModule.DevicesName.Projector2Screen)

        'Steven
        For Each s As Source In PublicModule.Sources
            's.ChangeVolume(PublicModule.SourceGainDef)
            s.SetVolumeMuted(Source.AudioDSPMute.Muted)
            s.SetPreviewMute(Source.AudioDSPMute.Muted)
            's.MaualMute = False
            's.PreviewMaualMute = False
        Next

        chbLecternMic.Checked = True
        chbWirelessMIC01.Checked = True
        chbWirelessMIC02.Checked = True
        chbWirelessMIC03.Checked = True
        chbWirelessMIC04.Checked = True
        MicroPhoneMute_Click(chbLecternMic, New System.EventArgs)
        MicroPhoneMute_Click(chbWirelessMIC01, New System.EventArgs)
        MicroPhoneMute_Click(chbWirelessMIC02, New System.EventArgs)
        MicroPhoneMute_Click(chbWirelessMIC03, New System.EventArgs)
        MicroPhoneMute_Click(chbWirelessMIC04, New System.EventArgs)

        '' Steven add 03042014
        OpenTV()
    End Sub

    Private Sub RequestReceiveSuccessInvoke(ByVal Sending_ID As Integer) '共同接收发送成功 Invoke
        OverFlowLayoutVisable(False)
        SourceLayoutVisable(False)
        rbtnMicroPhones.Visible = False
        chbExamMode.Visible = False
        VolumeLayoutVisable(True)
        ProjectorLayoutVisable(False)
        OverFlowReceiveMode = True
        btnOverFlowEngagedDisconnect.Text = "Disconnect" & vbCrLf & "from" & vbCrLf & PublicModule.Overflowdisconnectfrom
        'btnOverFlowEngagedDisconnect.Text = "Disconnect" & vbCrLf & "from" & vbCrLf & PublicModule.GetRoomNameFromID(Sending_ID)
        rbtnOverFlow.Text = "OverFlow" & vbCrLf & "(Receiving)"
        rbtnOverFlow.ForeColor = Color.Black
        rbtnOverFlow.BackgroundImage = My.Resources.NewSkin.Background_BrightRed

        OverFlowRoomButtons(Sending_ID).Text = PublicModule.GetRoomNameFromID(Sending_ID) & vbCrLf & "(Sending)"
        OverFlowRoomButtons(Sending_ID).ForeColor = Color.Black
        OverFlowRoomButtons(Sending_ID).BackgroundImage = My.Resources.NewSkin.Background_Grey

        Dim PreSourceSelectSave() As Integer = PreSourceSelect
        Dim ProjectorUseSave() As Integer = ProjectorUse

        'DeviceCon.SendMessage(Source.VedioInput.VCPrimary & "*" & Source.VedioOuput.Projector1 & "!", DevicesName.VideoMatrixSwitcher)
        'DeviceCon.SendMessage(Source.VedioInput.VCSecondary & "*" & Source.VedioOuput.Projector2 & "!", DevicesName.VideoMatrixSwitcher)

        PublicModule.RoomClasses(4).SetVolumeMuted(Room.AudioDSPMute.UnMuted)
        PublicModule.RoomClasses(4).PreviewMuted = True

        If PreRoomSelect = 4 Then
            chbVolumeMuted.Checked = PublicModule.RoomClasses(4).VolumeMuted
            chbLecternMuted.Checked = PublicModule.RoomClasses(4).PreviewMuted
        End If

        DeviceCon.SendMessage(Source.VedioOuput.Projector1 & "*0B", DevicesName.VideoMatrixSwitcher)
        DeviceCon.SendMessage(Source.VedioOuput.RecordingDigitizer & "*0B", DevicesName.VideoMatrixSwitcher)
        PublicModule.Projector1Control.ProjectorCommandSent(ProjectorTcpControl.Action.Power_On)
        chbProjector1Power.Checked = False
        PublicModule.Projector1Control.ProjectorCommandSent(ProjectorTcpControl.Action.AV_Mute_Off)
        PublicModule.Projector1Control.ProjectorCommandSent(ProjectorTcpControl.Action.Input_HDMI)
        DeviceCon.SendMessage("33 1" & vbCrLf, PublicModule.DevicesName.Projector1Screen)

        DeviceCon.SendMessage(Source.VedioOuput.Projector2 & "*0B", DevicesName.VideoMatrixSwitcher)
        PublicModule.Projector2Control.ProjectorCommandSent(ProjectorTcpControl.Action.Power_On)
        chbProjector2Power.Checked = False
        PublicModule.Projector2Control.ProjectorCommandSent(ProjectorTcpControl.Action.AV_Mute_Off)
        PublicModule.Projector2Control.ProjectorCommandSent(ProjectorTcpControl.Action.Input_HDMI)
        DeviceCon.SendMessage("33 1" & vbCrLf, PublicModule.DevicesName.Projector2Screen)

        For Each s As Source In PublicModule.Sources
            's.ChangeVolume(PublicModule.SourceGainDef)
            s.SetVolumeMuted(Source.AudioDSPMute.Muted)
            s.SetPreviewMute(Source.AudioDSPMute.Muted)
            's.MaualMute = False
            's.PreviewMaualMute = False
        Next

        chbLecternMic.Checked = True
        chbWirelessMIC01.Checked = True
        chbWirelessMIC02.Checked = True
        chbWirelessMIC03.Checked = True
        chbWirelessMIC04.Checked = True
        MicroPhoneMute_Click(chbLecternMic, New System.EventArgs)
        MicroPhoneMute_Click(chbWirelessMIC01, New System.EventArgs)
        MicroPhoneMute_Click(chbWirelessMIC02, New System.EventArgs)
        MicroPhoneMute_Click(chbWirelessMIC03, New System.EventArgs)
        MicroPhoneMute_Click(chbWirelessMIC04, New System.EventArgs)

    End Sub

    Private Sub RequestReceiveDisconnectSuccessInvoke(ByVal Sending_ID As Integer) '请求断开接收成功 Invoke
        OverFlowLayoutVisable(True)
        SourceLayoutVisable(True)
        rbtnMicroPhones.Visible = True
        chbExamMode.Visible = True

        If PreRoomSelect = 4 Then
            VolumeLayoutVisable(False)
        End If

        ProjectorLayoutVisable(True)
        OverFlowReceiveMode = False
        rbtnOverFlow.Text = "OverFlow"

        If PreRoomSelect = 4 Then
            rbtnOverFlow.BackgroundImage = PublicModule.GetSkinImage(PublicModule.RoomButtonPressColor)
        Else
            rbtnOverFlow.BackgroundImage = PublicModule.GetSkinImage(PublicModule.CommonButtonColor)
            rbtnOverFlow.ForeColor = Color.White
        End If

        'If tlpRoomStates.Visible Then
        '    OverFlowRoomButtons(Sending_ID).Text = PublicModule.GetRoomNameFromID(Sending_ID) & vbCrLf & "(Stand Alone)"
        '    OverFlowRoomButtons(Sending_ID).BackgroundImage = My.Resources.NewSkin.Background_DeepBlue
        '    OverFlowRoomButtons(Sending_ID).ForeColor = Color.White
        'End If


        btnProjector1.Text = "No Show" '按钮显示没信号
        CloseTV() 'zxg
        btnProjector2.Text = "No Show"
        btnProjector1.BackgroundImage = PublicModule.GetSkinImage(PublicModule.CommonButtonColor)
        btnProjector2.BackgroundImage = PublicModule.GetSkinImage(PublicModule.CommonButtonColor)
        btnProjector1.ForeColor = Color.White
        btnProjector2.ForeColor = Color.White
        ProjectorUse(0) = -1
        ProjectorUse(1) = -1

        PublicModule.Projector1Control.ProjectorCommandSent(ProjectorTcpControl.Action.AV_Mute_On)
        PublicModule.Projector2Control.ProjectorCommandSent(ProjectorTcpControl.Action.AV_Mute_On)

        PublicModule.RoomClasses(4).SetVolumeMuted(Room.AudioDSPMute.Muted)

        'zxg disconnect,cut output 4&5
        DeviceCon.SendMessage("0*" & Source.VedioOuput.Projector1 & "!", DevicesName.VideoMatrixSwitcher)
        DeviceCon.SendMessage("0*" & Source.VedioOuput.RecordingDigitizer & "!", DevicesName.VideoMatrixSwitcher)
        DeviceCon.SendMessage("0*" & Source.VedioOuput.Projector2 & "!", DevicesName.VideoMatrixSwitcher)
        ' steven close tv
        CloseTV()

    End Sub

    Private Sub StandAlone_Send_Receive_Click(ByVal sender As Object, ByVal e As EventArgs) Handles rbtnOverFlowSend.Click, rbtnOverFlowReceive.Click, rbtnOverFlowStandAlone.Click
        '按下Send Receive后更新面板
        Dim MyOFR As OverFlowRoom = PublicModule.GetOverFlowRoomFromID(PublicModule.ThisOverFlowRoomID)
        If rbtnOverFlowSend.Checked Or rbtnOverFlowReceive.Checked Then
            If tlpOverFlowMode.Visible Then 'Send面板

                'zxg
                SendingSetInvoke(PublicModule.ThisOverFlowRoomID, 0)

                For i As Integer = 0 To PublicModule.OverFlowRoomCount - 1
                    Dim OFR As OverFlowRoom = PublicModule.GetOverFlowRoomFromID(i)
                    If OFR.ID <> PublicModule.ThisOverFlowRoomID Then '本房间按钮不作更改
                        Select Case OFR.RoomState

                            Case OverFlowRoom.MyRoomState.StandAlone
                                OverFlowRoomButtons(i).Text = OFR.RoomName & vbCrLf & "(Stand Alone)"
                                OverFlowRoomButtons(i).BackgroundImage = My.Resources.NewSkin.Background_White
                            Case OverFlowRoom.MyRoomState.Waiting
                                If OFR.Waiting.Contains(MyOFR.ID) Then '此房间是本房间发送的请求
                                    OverFlowRoomButtons(i).Text = OFR.RoomName & vbCrLf & "(Waiting)"
                                    OverFlowRoomButtons(i).BackgroundImage = My.Resources.NewSkin.Background_Orange
                                Else
                                    OverFlowRoomButtons(i).Text = OFR.RoomName & vbCrLf & "(Waiting)"
                                    OverFlowRoomButtons(i).BackgroundImage = My.Resources.NewSkin.Background_Grey
                                End If
                            Case OverFlowRoom.MyRoomState.Sending
                                OverFlowRoomButtons(i).Text = OFR.RoomName & vbCrLf & "(Sending)"
                                OverFlowRoomButtons(i).BackgroundImage = My.Resources.NewSkin.Background_Grey
                            Case OverFlowRoom.MyRoomState.Requesting
                                OverFlowRoomButtons(i).Text = OFR.RoomName & vbCrLf & "(Sending)"
                                OverFlowRoomButtons(i).BackgroundImage = My.Resources.NewSkin.Background_Grey
                            Case OverFlowRoom.MyRoomState.Receiving
                                If MyOFR.Sending.Contains(OFR.ID) Then '此房间是本房间正在发送的
                                    OverFlowRoomButtons(i).Text = OFR.RoomName & vbCrLf & "(Receiving)"
                                    OverFlowRoomButtons(i).BackgroundImage = My.Resources.NewSkin.Background_BrightRed
                                Else
                                    OverFlowRoomButtons(i).Text = OFR.RoomName & vbCrLf & "(Receiving)"
                                    OverFlowRoomButtons(i).BackgroundImage = My.Resources.NewSkin.Background_Grey
                                End If

                        End Select
                    End If
                Next
            Else 'Receive

                'zxg
                If (Not OverFlowReceiveMode) Then
                    If (PublicModule.ThisOverFlowRoomID = 2) Then
                        AcceptReceiveSuccessInvoke(3)
                    Else
                        AcceptReceiveSuccessInvoke(2)
                    End If

                End If

                For i As Integer = 0 To PublicModule.OverFlowRoomCount - 1
                    Dim OFR As OverFlowRoom = PublicModule.GetOverFlowRoomFromID(i)
                    If OFR.ID <> PublicModule.ThisOverFlowRoomID Then
                        Select Case OFR.RoomState

                            Case OverFlowRoom.MyRoomState.StandAlone
                                OverFlowRoomButtons(i).Text = OFR.RoomName & vbCrLf & "(Stand Alone)"
                                OverFlowRoomButtons(i).BackgroundImage = My.Resources.NewSkin.Background_White
                            Case OverFlowRoom.MyRoomState.Waiting
                                OverFlowRoomButtons(i).Text = OFR.RoomName & vbCrLf & "(Waiting)"
                                OverFlowRoomButtons(i).BackgroundImage = My.Resources.NewSkin.Background_Grey
                            Case OverFlowRoom.MyRoomState.Sending
                                If MyOFR.Waiting.Contains(OFR.ID) Then
                                    OverFlowRoomButtons(i).Text = OFR.RoomName & vbCrLf & "(Sending)"
                                    OverFlowRoomButtons(i).BackgroundImage = My.Resources.NewSkin.Background_Orange
                                Else
                                    OverFlowRoomButtons(i).Text = OFR.RoomName & vbCrLf & "(Sending)"
                                    OverFlowRoomButtons(i).BackgroundImage = My.Resources.NewSkin.Background_Grey
                                End If
                            Case OverFlowRoom.MyRoomState.Requesting
                                If MyOFR.Waiting.Contains(OFR.ID) Then
                                    OverFlowRoomButtons(i).Text = OFR.RoomName & vbCrLf & "(Sending)"
                                    OverFlowRoomButtons(i).BackgroundImage = My.Resources.NewSkin.Background_Orange
                                Else
                                    OverFlowRoomButtons(i).Text = OFR.RoomName & vbCrLf & "(Sending)"
                                    OverFlowRoomButtons(i).BackgroundImage = My.Resources.NewSkin.Background_Grey
                                End If
                            Case OverFlowRoom.MyRoomState.Receiving
                                OverFlowRoomButtons(i).Text = OFR.RoomName & vbCrLf & "(Receiving)"
                                OverFlowRoomButtons(i).BackgroundImage = My.Resources.NewSkin.Background_Grey

                        End Select
                    End If
                Next
            End If
        ElseIf rbtnOverFlowStandAlone.Checked Then '按StandAlone根据情况中断Overflow的东西



            Select Case MyOFR.RoomState '根据各种情况而定

                Case OverFlowRoom.MyRoomState.Receiving
                    btnOverFlowEngagedDisconnect_Click(btnOverFlowEngagedDisconnect, New EventArgs)
                Case OverFlowRoom.MyRoomState.Requesting
                    MyOFR.CommandSend(MyOFR.CommandMaker(OverFlowRoom.Commands.REQUEST_BACK_TO_STANDLONE))
                Case OverFlowRoom.MyRoomState.Sending
                    MyOFR.CommandSend(MyOFR.CommandMaker(OverFlowRoom.Commands.REQUEST_BACK_TO_STANDLONE))
                Case OverFlowRoom.MyRoomState.Waiting
                    MyOFR.CommandSend(MyOFR.CommandMaker(OverFlowRoom.Commands.REQUEST_BACK_TO_STANDLONE))
            End Select

            'zxg

            If (OverFlowReceiveMode) Then
                btnOverFlowEngagedDisconnect_Click(btnOverFlowEngagedDisconnect, New EventArgs)
            End If

            StandAloneSetInvoke(PublicModule.ThisOverFlowRoomID)

        End If
    End Sub

    Private Sub OverFlowRoomsButtons_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnCaseRoom1.Click, _
                                                                                                 btnCaseRoom2.Click, _
                                                                                                 btnCaseRoom3.Click, _
                                                                                                 btnCaseRoom4.Click, _
                                                                                                 btnOGGB3.Click, _
                                                                                                 btnOGGB4.Click, _
                                                                                                 btnOGGB5.Click, _
                                                                                                 btn098.Click, _
                                                                                                 btnFAndP.Click
        Dim OFRBC As Button = sender '被按的按钮
        Dim CID As Integer = Array.IndexOf(OverFlowRoomButtons, OFRBC) '被按按钮对应的ID
        Dim OFR As OverFlowRoom = PublicModule.GetOverFlowRoomFromID(CID) '被按按钮的ID对应房间对象
        Dim MyOFR As OverFlowRoom = PublicModule.GetOverFlowRoomFromID(PublicModule.ThisOverFlowRoomID) '自己房间对象
        If tlpOverFlowMode.Visible Then 'Send面板
            If OFR.LineState And OFR.RoomState = OverFlowRoom.MyRoomState.StandAlone And OFR.ID <> PublicModule.ThisOverFlowRoomID Then
                MyOFR.CommandSend(MyOFR.CommandMaker(OverFlowRoom.Commands.REQUEST_SEND_, OFR.ID))
            ElseIf Not OFR.LineState And OFR.ID <> PublicModule.ThisOverFlowRoomID Then '对方不在线
                MessageBox.Show(OFR.RoomName & " Offline!!!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        Else 'Receive面板
            If MyOFR.RoomState <> OverFlowRoom.MyRoomState.Sending And MyOFR.Waiting.Contains(CID) And OFR.RoomState = OverFlowRoom.MyRoomState.Requesting And OFR.LineState And OFR.ID <> PublicModule.ThisOverFlowRoomID Then
                MyOFR.CommandSend(MyOFR.CommandMaker(OverFlowRoom.Commands.ACCEPT_REVEIVE_, OFR.ID))
            ElseIf MyOFR.RoomState <> OverFlowRoom.MyRoomState.Sending And MyOFR.Waiting.Contains(CID) And OFR.RoomState = OverFlowRoom.MyRoomState.Sending And OFR.LineState And OFR.ID <> PublicModule.ThisOverFlowRoomID Then
                MyOFR.CommandSend(MyOFR.CommandMaker(OverFlowRoom.Commands.ACCEPT_REVEIVE_, OFR.ID))
            ElseIf (OFR.RoomState = OverFlowRoom.MyRoomState.Sending Or OFR.RoomState = OverFlowRoom.MyRoomState.StandAlone) And OFR.LineState And OFR.ID <> PublicModule.ThisOverFlowRoomID Then '对方正在传输，或无事，不是传输给本房间
                Dim dialog As New PasswordDialog(PasswordDialog.Action.Overflow)
                If dialog.ShowDialog = Windows.Forms.DialogResult.Yes Then
                    MyOFR.CommandSend(MyOFR.CommandMaker(OverFlowRoom.Commands.REQUEST_RECEIVE_, OFR.ID))
                End If
            End If
        End If

    End Sub

    Private Sub btnOverFlowEngagedDisconnect_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnOverFlowEngagedDisconnect.Click
        '断开接收
        Dim MyOFR As OverFlowRoom = PublicModule.GetOverFlowRoomFromID(PublicModule.ThisOverFlowRoomID)
        Dim OFR As OverFlowRoom = PublicModule.GetOverFlowRoomFromID(MyOFR.Receiving(0))
        If OFR Is Nothing Then 'ControlRoom
            MyOFR.CommandSend(MyOFR.CommandMaker(OverFlowRoom.Commands.REQUEST_RECEIVE_DISCONNECT_, PublicModule.OverFlowRoomCount))
        Else 'OtherRoom
            MyOFR.CommandSend(MyOFR.CommandMaker(OverFlowRoom.Commands.REQUEST_RECEIVE_DISCONNECT_, OFR.ID))
        End If

        'zxg
        RequestReceiveDisconnectSuccessInvoke(PublicModule.ThisOverFlowRoomID)

        'zxg  back to standalone
        rbtnOverFlowStandAlone.Checked = True
        OverFlowStaterbtnButon_CheckChange(rbtnOverFlowStandAlone, New EventArgs)
        StandAlone_Send_Receive_Click(rbtnOverFlowStandAlone, New EventArgs)

    End Sub





    '**********************************************************************************

    '********************************SkinChange****************************************

    Private Sub SourceButtonChecked(ByVal sender As System.Object, ByVal e As EventArgs) Handles rbtnSource1.CheckedChanged, _
                                                                                                 rbtnSource2.CheckedChanged, _
                                                                                                 rbtnSource3.CheckedChanged, _
                                                                                                 rbtnSource4.CheckedChanged, _
                                                                                                 rbtnSource5.CheckedChanged
        Dim SBC As RadioButton = sender '信号源按钮皮肤更换
        If SBC.Checked Then
            'SBC.BackgroundImage = My.Resources.Skin.SourceRadioButtonCheck  '皮肤更换
            SBC.BackgroundImage = PublicModule.GetSkinImage(PublicModule.SourceAndProjectorButtonPressColor)
            SBC.ForeColor = Color.Black
        Else
            'SBC.BackgroundImage = My.Resources.Skin.AllButton '皮肤更换
            SBC.BackgroundImage = PublicModule.GetSkinImage(PublicModule.CommonButtonColor)
            SBC.ForeColor = Color.White
        End If

    End Sub

    Private Sub RoomButtonChecked(ByVal sender As System.Object, ByVal e As EventArgs) Handles rbtnEndSession.CheckedChanged, _
                                                                                               rbtnMicroPhones.CheckedChanged, _
                                                                                               rbtnScreensDisplays.CheckedChanged, _
                                                                                               rbtnLighting.CheckedChanged, _
                                                                                               rbtnOverFlow.CheckedChanged
        Dim RBC As RadioButton = sender 'Room按钮皮肤更换
        If RBC.Checked Then
            'RBC.BackgroundImage = My.Resources.Skin.RoomRadioButtonCheck '皮肤更换
            If RBC.Equals(rbtnOverFlow) And (OverFlowSendMode Or OverFlowReceiveMode) Then '在OverFlow发送和接收模式都要维持颜色
                RBC.BackgroundImage = My.Resources.NewSkin.Background_BrightRed
            Else
                RBC.BackgroundImage = PublicModule.GetSkinImage(PublicModule.RoomButtonPressColor)
            End If
            RBC.ForeColor = Color.Black
        Else
            'RBC.BackgroundImage = My.Resources.Skin.AllButton '皮肤更换
            If RBC.Equals(rbtnOverFlow) And (OverFlowSendMode Or OverFlowReceiveMode) Then '在OverFlow发送和接收模式都要维持颜色
                RBC.BackgroundImage = My.Resources.NewSkin.Background_BrightRed
            Else
                RBC.BackgroundImage = PublicModule.GetSkinImage(PublicModule.CommonButtonColor)
                RBC.ForeColor = Color.White
            End If
        End If

    End Sub

    Private Sub VolumeCheckedChange(ByVal sender As System.Object, ByVal e As EventArgs) Handles chbVolumeMuted.CheckedChanged, _
                                                                                                 chbLecternMic.CheckedChanged, _
                                                                                                 chbWirelessMIC01.CheckedChanged, _
                                                                                                 chbWirelessMIC02.CheckedChanged, _
                                                                                                 chbWirelessMIC03.CheckedChanged, _
                                                                                                 chbWirelessMIC04.CheckedChanged, _
                                                                                                 chbLecternMuted.CheckedChanged
        Dim VCC As CheckBox = sender '所有静音按钮皮肤更换
        If VCC.Checked Then
            VCC.BackgroundImage = My.Resources.NewSkin.Muted
            VCC.Text = Nothing
        Else
            VCC.BackgroundImage = My.Resources.NewSkin.UnMuted
            VCC.Text = "Live"
        End If
    End Sub

    Private Sub SourcePageButtonSkinChangeMouseDown(ByVal sender As Object, ByVal e As EventArgs) Handles btnPageUp.MouseDown, _
                                                                                                          btnPageDown.MouseDown '翻页按钮按下
        Dim SPBSC As Button = sender
        Select Case SPBSC.Name
            Case btnPageUp.Name
                SPBSC.BackgroundImage = PublicModule.GetSkinImage(PublicModule.SourceAndProjectorButtonPressColor)
                SPBSC.ForeColor = Color.Black
            Case btnPageDown.Name
                SPBSC.BackgroundImage = PublicModule.GetSkinImage(PublicModule.SourceAndProjectorButtonPressColor)
                SPBSC.ForeColor = Color.Black
        End Select
    End Sub

    Private Sub SourcePageButtonSkinChangeMouseUp(ByVal sender As Object, ByVal e As EventArgs) Handles btnPageUp.MouseUp, _
                                                                                                        btnPageDown.MouseUp '翻页按钮松开
        Dim SPBSC As Button = sender
        Select Case SPBSC.Name
            Case btnPageUp.Name
                SPBSC.BackgroundImage = PublicModule.GetSkinImage(PublicModule.CommonButtonColor)
                SPBSC.ForeColor = Color.White
            Case btnPageDown.Name
                SPBSC.BackgroundImage = PublicModule.GetSkinImage(PublicModule.CommonButtonColor)
                SPBSC.ForeColor = Color.White
        End Select
    End Sub

    Private Sub chbExamMode_CheckedChange(ByVal sender As Object, ByVal e As EventArgs) Handles chbExamMode.CheckedChanged 'ExamMode按钮皮肤更换
        If chbExamMode.Checked Then
            chbExamMode.BackgroundImage = PublicModule.GetSkinImage(PublicModule.RoomButtonPressColor)
            chbExamMode.ForeColor = Color.Black
        Else
            chbExamMode.BackgroundImage = PublicModule.GetSkinImage(PublicModule.CommonButtonColor)
            chbExamMode.ForeColor = Color.White
        End If
    End Sub

    Private Sub AnnotationButtonSkinChange(ByVal sender As Object, ByVal e As EventArgs) Handles rbtnAnnotationAuxAVInput.CheckedChanged, _
                                                                                                 rbtnAnnotationVCPrimary.CheckedChanged, _
                                                                                                 rbtnAnnotationVCSecondary.CheckedChanged, _
                                                                                                 rbtnAnnotationDocumentCamera.CheckedChanged, _
                                                                                                 rbtnAnnotationGuestDevices.CheckedChanged, _
                                                                                                 rbtnAnnotationLecternComputer.CheckedChanged, _
                                                                                                 rbtnAnnotationPresenterCamera.CheckedChanged, _
                                                                                                 rbtnWhiteBoard.CheckedChanged 'Annotation面板按钮皮肤改变
        Dim ABSC As RadioButton = sender
        Select Case ABSC.Checked

            Case False
                ABSC.BackgroundImage = My.Resources.NewSkin.Background_OxbloodRed_NoBorder
                ABSC.ForeColor = Color.White
            Case True
                ABSC.BackgroundImage = PublicModule.GetSkinImage(PublicModule.RoomButtonPressColor)
                ABSC.ForeColor = Color.Black
        End Select

    End Sub

    Private Sub DocumentCameraButtonSkinChangeMouseUp(ByVal sender As Object, ByVal e As EventArgs) Handles btnDocumentCameraZoomIn.MouseUp, _
                                                                                                           btnDocumentCameraZoomOut.MouseUp 'DocumentCameraButton松开
        Dim DCBSC As Button = sender
        Select Case DCBSC.Name
            Case btnDocumentCameraZoomIn.Name
                DCBSC.BackgroundImage = My.Resources.NewSkin.ArrowUp_Black
            Case btnDocumentCameraZoomOut.Name
                DCBSC.BackgroundImage = My.Resources.NewSkin.ArrowDown_Black
            Case Else
                DCBSC.BackgroundImage = My.Resources.NewSkin.Background_White
        End Select
    End Sub

    Private Sub DocumentCameraButtonSkinChangeMouseDown(ByVal sender As Object, ByVal e As EventArgs) Handles btnDocumentCameraZoomIn.MouseDown, _
                                                                                                       btnDocumentCameraZoomOut.MouseDown 'DocumentCameraButton按下
        Dim DCBSC As Button = sender
        Select Case DCBSC.Name
            Case btnDocumentCameraZoomIn.Name
                DCBSC.BackgroundImage = My.Resources.NewSkin.ArrowUp_BlackChecked
            Case btnDocumentCameraZoomOut.Name
                DCBSC.BackgroundImage = My.Resources.NewSkin.ArrowDown_BlackChecked
            Case Else
                DCBSC.BackgroundImage = PublicModule.GetSkinImage(PublicModule.RoomButtonPressColor)
        End Select
    End Sub

    Private Sub DocumentCameraRadioButton_CheckChange(ByVal sender As Object, ByVal e As EventArgs) Handles rbtnDocumentCameraFreeze.CheckedChanged, _
                                                                                                            rbtnDocumentCameraRelease.CheckedChanged, _
                                                                                                            rbtnDocumentCameraLightOFF.CheckedChanged, _
                                                                                                            rbtnDocumentCameraLightUpper.CheckedChanged, _
                                                                                                            rbtnDocumentCameraPortrait.CheckedChanged, _
                                                                                                            rbtnDocumentCameraLandscape.CheckedChanged 'DocumentCameraRadioButton状态改变换皮肤
        Dim DCRB As RadioButton = sender
        If DCRB.Checked Then
            DCRB.BackgroundImage = My.Resources.NewSkin.Background_Yellow
        Else
            DCRB.BackgroundImage = My.Resources.NewSkin.Background_White
        End If

    End Sub

    Private Sub rbtnLightSkinChange_CheckedChange(ByVal sender As Object, ByVal e As EventArgs) Handles rbtnLightWelcome.CheckedChanged, _
                                                                                                              rbtnLightTeach.CheckedChanged, _
                                                                                                              rbtnLightQualityProjection.CheckedChanged, _
                                                                                                              rbtnLightExtraQuality.CheckedChanged, _
                                                                                                              rbtnLightBlackOut.CheckedChanged 'Light面板单选按钮皮肤更换
        Dim LSC As RadioButton = sender
        If LSC.Checked Then
            LSC.BackgroundImage = PublicModule.GetSkinImage(PublicModule.RoomButtonPressColor)
            'LSC.ForeColor = Color.Black
        Else
            LSC.BackgroundImage = My.Resources.NewSkin.Background_White
            'LSC.ForeColor = Color.White
        End If


    End Sub

    Private Sub chbLightSkinChange_CheckChange(ByVal sender As Object, ByVal e As EventArgs) Handles chbBoardLights1.CheckedChanged, _
                                                                                                    chbBoardLights2.CheckedChanged, _
                                                                                                    chbBoardLights3.CheckedChanged, _
                                                                                                    chbPresenterSpots1.CheckedChanged, _
                                                                                                    chbPresenterSpots2.CheckedChanged, _
                                                                                                    chbAuxiluarySpots1.CheckedChanged, _
                                                                                                    chbAuxiluarySpots2.CheckedChanged  '灯光面板多选按钮皮肤更换
        Dim LSC As CheckBox = sender
        If LSC.Checked Then
            LSC.BackgroundImage = PublicModule.GetSkinImage(PublicModule.RoomButtonPressColor)
            'LSC.ForeColor = Color.Black
        Else
            LSC.BackgroundImage = My.Resources.NewSkin.Background_White
            'LSC.ForeColor = Color.White
        End If
    End Sub

    Private Sub OverFlowStaterbtnButon_CheckChange(ByVal sender As Object, ByVal e As EventArgs) Handles rbtnOverFlowStandAlone.CheckedChanged, _
                                                                                                         rbtnOverFlowSend.CheckedChanged, _
                                                                                                         rbtnOverFlowReceive.CheckedChanged, _
                                                                                                         rbtnOverFlowModeSingle.CheckedChanged, _
                                                                                                         rbtnOverFlowModeDouble.CheckedChanged 'OverFlow面板单选按钮皮肤更换
        Dim OFSB As RadioButton = sender
        If OFSB.Checked Then
            Select Case OFSB.Name
                Case rbtnOverFlowStandAlone.Name
                    lbRoomStates.Visible = False
                    tlpRoomStates.Visible = False
                    lbOverFlowMode.Visible = False
                    tlpOverFlowMode.Visible = False
                Case rbtnOverFlowSend.Name
                    'zxg
                    'lbRoomStates.Visible = True
                    'tlpRoomStates.Visible = True
                    lbRoomStates.Visible = False
                    tlpRoomStates.Visible = False
                    lbOverFlowMode.Visible = True
                    tlpOverFlowMode.Visible = True
                Case rbtnOverFlowReceive.Name
                    'zxg
                    'lbRoomStates.Visible = True
                    'tlpRoomStates.Visible = True
                    lbRoomStates.Visible = False
                    tlpRoomStates.Visible = False
                    lbOverFlowMode.Visible = False
                    tlpOverFlowMode.Visible = False
                Case rbtnOverFlowModeSingle.Name
                    If OFSB.Checked Then
                        If ProjectorUse(0) <> -1 Then
                            PublicModule.Sources(ProjectorUse(0)).VedioSwitch(Source.VedioOuput.OverFlowPrimary)
                        Else
                            DeviceCon.SendMessage("0*" & Source.VedioOuput.OverFlowPrimary & "!", PublicModule.DevicesName.VideoMatrixSwitcher)
                        End If
                        DeviceCon.SendMessage(Source.VedioInput.PTZRoomCamera & "*" & Source.VedioOuput.OverFlowSecondary & "!", DevicesName.VideoMatrixSwitcher)

                        If ProjectorUse(0) <> -1 And ProjectorUse(1) <> ProjectorUse(0) And OverFlowSendMode Then '如果projector1 与 projector2 有输出,不一致,那么projector1 与proejctor2 一致
                            Dim SelectSave() As Integer = PreSourceSelect

                            For i As Integer = 0 To SourceIDPage.GetUpperBound(0)
                                For j As Integer = 0 To SourceIDPage.GetUpperBound(1)
                                    If SourceIDPage(i, j) = ProjectorUse(0) Then
                                        PreSourceSelect(0) = i
                                        PreSourceSelect(1) = j
                                    End If
                                Next
                            Next

                            btnProjector_Click(btnProjector2, New EventArgs)

                            PreSourceSelect = SelectSave
                        ElseIf ProjectorUse(0) = -1 And ProjectorUse(1) <> -1 And OverFlowSendMode Then

                            Dim SelectSave() As Integer = PreSourceSelect

                            For i As Integer = 0 To SourceIDPage.GetUpperBound(0)
                                For j As Integer = 0 To SourceIDPage.GetUpperBound(1)
                                    If SourceIDPage(i, j) = ProjectorUse(1) Then
                                        PreSourceSelect(0) = i
                                        PreSourceSelect(1) = j
                                    End If
                                Next
                            Next

                            btnProjector_Click(btnProjector1, New EventArgs)

                            PreSourceSelect = SelectSave

                        End If

                        If OverFlowSendMode Then
                            btnProjector2.Enabled = False
                        End If
                    End If
                Case rbtnOverFlowModeDouble.Name
                    If OFSB.Checked Then
                        If ProjectorUse(0) <> -1 Then
                            PublicModule.Sources(ProjectorUse(0)).VedioSwitch(Source.VedioOuput.OverFlowPrimary)
                        Else
                            DeviceCon.SendMessage("0*" & Source.VedioOuput.OverFlowPrimary & "!", PublicModule.DevicesName.VideoMatrixSwitcher)
                        End If
                        If ProjectorUse(1) <> -1 Then
                            PublicModule.Sources(ProjectorUse(1)).VedioSwitch(Source.VedioOuput.OverFlowSecondary)
                        Else
                            DeviceCon.SendMessage("0*" & Source.VedioOuput.OverFlowSecondary & "!", PublicModule.DevicesName.VideoMatrixSwitcher)
                        End If

                        If OverFlowSendMode Then
                            btnProjector2.Enabled = True
                        End If

                    End If

            End Select
            OFSB.BackgroundImage = PublicModule.GetSkinImage(PublicModule.RoomButtonPressColor)
            OFSB.ForeColor = Color.Black
        Else
            OFSB.BackgroundImage = My.Resources.NewSkin.Background_White
            'OFSB.ForeColor = Color.White
        End If
    End Sub

    Private Sub ScreenDisplaySkinChange_MouseUp(ByVal sender As Object, ByVal e As EventArgs) Handles btnScreen1Stop.MouseUp, _
                                                                                                      btnScreen2Stop.MouseUp '幕布与投影机面板按钮松开
        Dim SDSC As Button = sender
        Select Case SDSC.Name
            Case btnScreen1Stop.Name
                SDSC.BackgroundImage = My.Resources.NewSkin.Background_White
            Case btnScreen2Stop.Name
                SDSC.BackgroundImage = My.Resources.NewSkin.Background_White
            Case Else
                SDSC.BackgroundImage = My.Resources.NewSkin.Background_DeepBlue
                SDSC.ForeColor = Color.White
        End Select
    End Sub

    Private Sub ScreenDisplaySkinChange_MouseDown(ByVal sender As Object, ByVal e As EventArgs) Handles btnScreen1Stop.MouseDown, _
                                                                                                  btnScreen2Stop.MouseDown  '幕布与投影机面板按钮按下
        Dim SDSC As Button = sender
        Select Case SDSC.Name
            Case btnScreen1Stop.Name
                SDSC.BackgroundImage = PublicModule.GetSkinImage(PublicModule.RoomButtonPressColor)
            Case btnScreen2Stop.Name
                SDSC.BackgroundImage = PublicModule.GetSkinImage(PublicModule.RoomButtonPressColor)
            Case Else
                SDSC.BackgroundImage = PublicModule.GetSkinImage(PublicModule.RoomButtonPressColor)
                SDSC.ForeColor = Color.Black
        End Select
    End Sub

    Private Sub rbtnScreensDisplaySkinChange_CheckChange(ByVal sender As Object, ByVal e As EventArgs) Handles rbtnScreen1Up.CheckedChanged, _
                                                                                                           rbtnScreen1Down.CheckedChanged, _
                                                                                                           rbtnScreen2Up.CheckedChanged, _
                                                                                                           rbtnScreen2Down.CheckedChanged '幕布与投影机面板单选按钮
        Dim SDSC As RadioButton = sender
        If SDSC.Checked Then
            Select Case SDSC.Name
                Case rbtnScreen1Up.Name
                    SDSC.BackgroundImage = My.Resources.NewSkin.ArrowUp_BlackChecked
                Case rbtnScreen1Down.Name
                    SDSC.BackgroundImage = My.Resources.NewSkin.ArrowDown_BlackChecked
                Case rbtnScreen2Up.Name
                    SDSC.BackgroundImage = My.Resources.NewSkin.ArrowUp_BlackChecked
                Case rbtnScreen2Down.Name
                    SDSC.BackgroundImage = My.Resources.NewSkin.ArrowDown_BlackChecked
            End Select
        Else
            Select Case SDSC.Name
                Case rbtnScreen1Up.Name
                    SDSC.BackgroundImage = My.Resources.NewSkin.ArrowUp_Black
                Case rbtnScreen1Down.Name
                    SDSC.BackgroundImage = My.Resources.NewSkin.ArrowDown_Black
                Case rbtnScreen2Up.Name
                    SDSC.BackgroundImage = My.Resources.NewSkin.ArrowUp_Black
                Case rbtnScreen2Down.Name
                    SDSC.BackgroundImage = My.Resources.NewSkin.ArrowDown_Black
            End Select
        End If
    End Sub

    Private Sub chbScreenDisplaySkinChange_CheckChange(ByVal sender As Object, ByVal e As EventArgs) Handles chbProjector1Power.CheckedChanged, _
                                                                                                             chbProjector2Power.CheckedChanged, _
                                                                                                             chbEventMode.CheckedChanged
        Dim SDSC As CheckBox = sender
        If SDSC.Checked Then
            SDSC.BackgroundImage = PublicModule.GetSkinImage(PublicModule.RoomButtonPressColor)
            SDSC.ForeColor = Color.Black
            Select Case SDSC.Name

                Case chbProjector1Power.Name
                    SDSC.Text = "Turn On Projector 1"
                Case chbProjector2Power.Name
                    SDSC.Text = "Turn On Projector 2"

            End Select

        Else
            SDSC.BackgroundImage = My.Resources.NewSkin.Background_DeepBlue
            SDSC.ForeColor = Color.White

            Select Case SDSC.Name

                Case chbProjector1Power.Name
                    SDSC.Text = "Turn Off Projector 1"
                Case chbProjector2Power.Name
                    SDSC.Text = "Turn Off Projector 2"

            End Select
        End If
    End Sub

    Private Sub RoomSetting_SkinChange_MouseDown(ByVal sender As Object, ByVal e As EventArgs) Handles btnRoomSetting.MouseDown, _
                                                                                            btnRoomSettingBack.MouseDown, _
                                                                                            btnRoomSettingSave.MouseDown, _
                                                                                            btnCloseTouchPanel.MouseDown
        Dim RSSC As Button = sender
        RSSC.BackgroundImage = PublicModule.GetSkinImage(PublicModule.RoomButtonPressColor)
        RSSC.ForeColor = Color.Black
    End Sub

    Private Sub RoomSetting_SkinChange_MouseUp(ByVal sender As Object, ByVal e As EventArgs) Handles btnRoomSetting.MouseUp, _
                                                                                                    btnRoomSettingBack.MouseUp, _
                                                                                                    btnRoomSettingSave.MouseUp, _
                                                                                                    btnCloseTouchPanel.MouseUp
        Dim RSSC As Button = sender
        RSSC.BackgroundImage = PublicModule.GetSkinImage(PublicModule.CommonButtonColor)
        RSSC.ForeColor = Color.White

        If String.Compare(RSSC.Name, btnRoomSetting.Name, False) = 0 Then
            If Event_Mode Then
                btnRoomSetting.BackgroundImage = PublicModule.GetSkinImage(PublicModule.RoomButtonPressColor)
                btnRoomSetting.ForeColor = Color.Black
            End If
        End If


    End Sub


    '**********************************************************************************

    '**************************TableLayoutPanel部分隐藏与调整**************************
    Private FunctionalLayoutSave(1) As Single    '保存投影机布局比例参数
    Private MainLayoutSave(3) As Single  '保存信号源布局比例参数
    Private SourceLayoutSave As Single
    Private VolumeLayoutSave As Single '保存音量条布局比例
    Private RoomSettingLayoutSave As Single '保存RoomSetting的布局比例
    Private OverFlowLayoutSave(3) As Single '保存OverFlow布局比例
    Private FunctionalPanelLayoutSave() As Single = {75.0, 25.0} '功能面板与音量条比例参数
    Private SourcesPageLayoutSave() As Single = {50.0, 50.0} '信号源翻页按钮布局比例参数

    Private Sub ProjectorLayoutVisable(ByVal b As Boolean) '投影机布局隐藏
        If b Then
            tlpFunctional.RowStyles(0).Height = FunctionalLayoutSave(0) '恢复比例
            tlpFunctional.RowStyles(1).Height = FunctionalLayoutSave(1)
        Else
            FunctionalLayoutSave(0) = tlpFunctional.RowStyles(0).Height  '保存参数
            FunctionalLayoutSave(1) = tlpFunctional.RowStyles(1).Height
            tlpFunctional.RowStyles(1).Height = 0
        End If
    End Sub

    Private Sub SourceLayoutVisable(ByVal b As Boolean) '信号源布局隐藏
        If b Then
            tlpMain.ColumnStyles(0).Width = SourceLayoutSave
        Else
            SourceLayoutSave = tlpMain.ColumnStyles(0).Width
            tlpMain.ColumnStyles(0).Width = 0
        End If
    End Sub

    'Private Sub VolumeLayoutVisable(ByVal b As Boolean) '音量条布局隐藏
    '    If b And tlpVolume.Visible Then
    '        tlpMain.ColumnStyles(2).Width = VolumeLayoutSave
    '    ElseIf Not b And Not tlpAnnotationSource.Visible Then
    '        VolumeLayoutSave = tlpMain.ColumnStyles(2).Width
    '        tlpMain.ColumnStyles(2).Width = 0
    '    End If
    'End Sub

    Private Sub RoomSettingLayoutVisable(ByVal b As Boolean) 'RoomSetting面板隐藏
        If b Then
            For i As Integer = 0 To MainLayoutSave.GetUpperBound(0)
                MainLayoutSave(i) = tlpMain.ColumnStyles(i).Width
            Next
            Me.Hide()
            tlpMain.ColumnStyles(0).Width = 0
            tlpMain.ColumnStyles(1).Width = 0
            tlpMain.ColumnStyles(2).Width = 0
            tlpMain.ColumnStyles(3).Width = 100
            Me.Show()
        Else
            Me.Hide()
            For i As Integer = 0 To MainLayoutSave.GetUpperBound(0)
                tlpMain.ColumnStyles(i).Width = MainLayoutSave(i)
            Next
            Me.Show()
        End If
    End Sub

    Private Sub OverFlowLayoutVisable(ByVal b As Boolean) 'OverFlow显示
        tacFunction.Visible = False

        If b Then
            For i As Integer = 0 To OverFlowLayoutSave.GetUpperBound(0)
                tlpOverFlow.RowStyles(i).Height = OverFlowLayoutSave(i)
            Next
            tlpOverFlowEngaged.Visible = Not b
        Else
            For i As Integer = 0 To OverFlowLayoutSave.GetUpperBound(0)
                OverFlowLayoutSave(i) = tlpOverFlow.RowStyles(i).Height
            Next
            For i As Integer = 0 To OverFlowLayoutSave.GetUpperBound(0)
                tlpOverFlow.RowStyles(i).Height = 0
            Next
            tlpOverFlowEngaged.Visible = Not b
        End If

        tacFunction.Visible = True
    End Sub

    Private Sub VolumeLayoutVisable(ByVal b As Boolean)

        If b And Not tlpVolume.Visible Then
            tlpFunctionalPanel.Visible = False
            tlpFunctionalPanel.ColumnStyles(0).Width = FunctionalPanelLayoutSave(0)
            tlpFunctionalPanel.ColumnStyles(1).Width = FunctionalPanelLayoutSave(1)
            tlpVolume.Visible = True
            tlpFunctionalPanel.Visible = True
        ElseIf Not b And tlpVolume.Visible Then
            tlpFunctionalPanel.Visible = False
            tlpVolume.Visible = False
            FunctionalPanelLayoutSave(0) = tlpFunctionalPanel.ColumnStyles(0).Width
            FunctionalPanelLayoutSave(1) = tlpFunctionalPanel.ColumnStyles(1).Width
            tlpFunctionalPanel.ColumnStyles(1).Width = 0
            tlpFunctionalPanel.Visible = True
        End If
    End Sub

    Private Sub SourcesPageButtonVisable(ByVal sender As Object, ByVal e As EventArgs) Handles btnPageUp.VisibleChanged, _
                                                                                               btnPageDown.VisibleChanged
        If btnPageUp.Visible And Not btnPageDown.Visible Then
            tlpPage.ColumnStyles(1).Width = 0
            tlpPage.ColumnStyles(0).Width = 100
        Else
            tlpPage.ColumnStyles(0).Width = 0
            tlpPage.ColumnStyles(1).Width = 100
        End If
    End Sub

    '**********************************************************************************

    Private Sub ExitNoTouchTimer_Tick(ByVal TimeOut As Long)  'TimeOut计时
        Threading.Thread.Sleep(TimeOut)
        Try
            PublicModule.LightCon.LightOff(True)
        Catch ex As Exception
            'MessageBox.Show(ex.Message, "Light Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
        Me.BeginInvoke(New EventHandler(AddressOf RoomButton_Click), New Object() {rbtnEndSession, New EventArgs})

        'RoomButton_Click(rbtnEndSession, New EventArgs) '按EndSession
    End Sub

    'zxg
    'Private Sub ExitNoTouchTimer_Tick_TV(ByVal TimeOut As Long)  'TimeOut计时
    '    Threading.Thread.Sleep(TimeOut)

    '    Tv.SendCommand(Tv.Action.POWEROFF)

    'End Sub
    Private Sub NoResponse_TV(ByVal TimeOut As Long)  'TimeOut计时
        Threading.Thread.Sleep(TimeOut)

        If (isTvChecking) Then
            Tv.SendCommand(Tv.Action.CHECK)
        End If
    End Sub

    Private Sub ExitNoTouchTimerReset()
        If Not Event_Mode And Not rbtnEndSession.Checked Then
            Try
                ExitNoTouchTimer.Abort()
            Catch ex As Exception

            End Try
            ExitNoTouchTimer = New Threading.Thread(AddressOf ExitNoTouchTimer_Tick)
            ExitNoTouchTimer.Start(PublicModule.NoOneTouchExitTime * 3600000)
            'ExitNoTouchTimer.Start(60000)
            'PublicModule.DebugMessageFileWrite("重置计时器")


            'zxg
            'Try
            '    ExitNoTouchTimerTv.Abort()
            'Catch ex As Exception

            'End Try
            'ExitNoTouchTimerTv = New Threading.Thread(AddressOf ExitNoTouchTimer_Tick_TV)
            'ExitNoTouchTimerTv.Start(7 * 60000)
        End If
    End Sub

    Private Sub MouseMoveCheck()
        While True
            Threading.Thread.Sleep(5000)
            If MouseMoveOrNot Then
                ExitNoTouchTimerReset()
                'Console.WriteLine("鼠标有移动")
                'PublicModule.DebugMessageFileWrite("鼠标有移动")
            Else
                'Console.WriteLine("鼠标无移动")
                'PublicModule.DebugMessageFileWrite("鼠标无移动")
            End If
            MouseMoveOrNot = False
        End While
    End Sub

    Private Sub GolbalMouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles MouseHookEvent.MouseActivity
        MouseMoveOrNot = True
    End Sub

    Private Sub ModeExitTimer_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ModeExitTimer.Tick '到了指定时间退出模式
        Dim NowTime As DateTime = My.Computer.Clock.LocalTime
        Dim CompareResult As Integer = PublicModule.ExamModeExitTime.CompareTo(NowTime)
        Dim Second As Integer = NowTime.Second - PublicModule.ExamModeExitTime.Second
        If CompareResult = 0 Then
            Console.WriteLine("到指定时间") '很小机会时间刚刚好
        ElseIf CompareResult > 0 Then
            'Console.WriteLine("未到指定时间")
        ElseIf CompareResult < 0 Then

            If Second >= 0 And Second <= 1 Then '线程和系统秒差
                Console.WriteLine("到指定时间")
                ExitNoTouchTimerReset()
                'PublicModule.ExamModeExitTime = PublicModule.ExamModeExitTime.AddDays(1) '加时
                '关闭相应Mode
            Else
                'Console.WriteLine("已过指定时间")
                PublicModule.ExamModeExitTime = PublicModule.ExamModeExitTime.AddDays(1) '加时
            End If

        End If

    End Sub

    Private Sub BlackOutTimer_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles BlackOutTimer.Tick  '到了指定时间控制灯光到BlackOut
        Dim NowTime As DateTime = My.Computer.Clock.LocalTime
        Dim CompareResult As Integer = PublicModule.BlackOutTime.CompareTo(NowTime)
        Dim Second As Integer = NowTime.TimeOfDay.Seconds - PublicModule.BlackOutTime.TimeOfDay.Seconds
        If CompareResult = 0 Then
            Console.WriteLine("到指定时间") '很小机会时间刚刚好
        ElseIf CompareResult > 0 Then
            'Console.WriteLine("未到指定时间")
        ElseIf CompareResult < 0 Then

            If Second >= 0 And Second <= 1 Then '线程和系统秒差
                Console.WriteLine("到指定时间")
                Try
                    PublicModule.LightCon.LightOff(False)
                Catch ex As Exception
                    'MessageBox.Show(ex.Message, "Light Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
                'PublicModule.BlackOutTime = PublicModule.BlackOutTime.AddDays(1) '加时
            Else
                Console.WriteLine("已过指定时间")
                PublicModule.BlackOutTime = PublicModule.BlackOutTime.AddDays(1) '加时
            End If

        End If

    End Sub

    Public WithEvents InfraredEquipmentSerialPortEnevts As System.IO.Ports.SerialPort

    Private Sub HumanCheck_SerialPortDataReceive() Handles InfraredEquipmentSerialPortEnevts.DataReceived
        '人体感应器的串口数据:
        '无人时:
        'F8 86 00 06 80 86 80 00 00 00 00 00 00 00 00 00 F8 E6 00
        '有人时:
        'F8 86 00 06 80 86 80 06 80 FE 00 00 00 00 00 00 00 F8 E6 00
        If Exam_Mode Then
            Dim CanRead As Integer = PublicModule.InfraredEquipmentSerialPort.BytesToRead '由于 SerialPort 类会缓冲数据，而 BaseStream 属性内包含的流则不缓冲数据，因此二者在可读字节数量上可能会不一致。 BytesToRead 属性可以指示有要读取的字节，但 BaseStream 属性中包含的流可能无法访问这些字节，原因是它们已缓冲到 SerialPort 类中。
            Dim rd(CanRead - 1) As Byte
            PublicModule.InfraredEquipmentSerialPort.Read(rd, 0, CanRead)
            Dim rds As String = System.BitConverter.ToString(rd)
            PublicModule.DebugMessageFileWrite("Infrared Equipment(Receive)： " & rds)

            Select Case rds

                Case "F8-86-00-06-80-86-80-00-00-00-00-00-00-00-00-00-F8-E6-00" '无人时


                Case "F8-86-00-06-80-86-80-06-80-FE-00-00-00-00-00-00-00-F8-E6-00" '有人时


            End Select





        End If
    End Sub

    Private Sub OpenInfraredEquipmentSerialPort() '打开人体检查串口
        Try
            PublicModule.InfraredEquipmentSerialPort.Open()
            InfraredEquipmentSerialPortEnevts = PublicModule.InfraredEquipmentSerialPort
        Catch ex As Exception
            MessageBox.Show(ex.Message, "InfraredEquipment Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Main_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles Me.KeyPress
        If String.Compare(e.KeyChar, Convert.ToChar(Keys.Escape), True) = 0 Then
            Me.FormBorderStyle = Windows.Forms.FormBorderStyle.Sizable
        End If
    End Sub

    Public Sub BetaVersionCheck() '测试版检测
        Dim check As New SHA512Managed
        Dim sum() As Byte
        sum = check.ComputeHash(Encoding.Default.GetBytes(PublicModule.BetaVersionRead.ToString & PublicModule.BetaTimeRead.ToString))

        If String.Compare(Convert.ToBase64String(sum), PublicModule.CodeCodeRead, False) = 0 Then
            If PublicModule.BetaVersionRead = True Then
                MessageBox.Show("This software is beta version, please buy the full version!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                BetaTimerUse.Interval = PublicModule.BetaTimeRead
                BetaTimerUse.Start()
            End If
        Else
            MessageBox.Show("This software is beta version, please buy the full version!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End
        End If

    End Sub

    Private Sub BetaUse(ByVal sender As Object, ByVal e As System.EventArgs) Handles BetaTimerUse.Tick
        MessageBox.Show("This software is beta version, please buy the full version!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
    End Sub

    Private Sub AnyTouch(ByVal sender As Object, ByVal e As System.EventArgs) Handles rbtnSource1.Click, _
                                                                                      rbtnSource2.Click, _
                                                                                      rbtnSource3.Click, _
                                                                                      rbtnSource4.Click, _
                                                                                      rbtnSource5.Click, _
                                                                                      btnPageDown.Click, _
                                                                                      btnPageUp.Click, _
                                                                                      btnRoomSetting.Click, _
                                                                                      rbtnMicroPhones.Click, _
                                                                                      rbtnScreensDisplays.Click, _
                                                                                      rbtnLighting.Click, _
                                                                                      rbtnOverFlow.Click, _
                                                                                      TrbVolume.Click, _
                                                                                      chbLecternMuted.Click, _
                                                                                      chbVolumeMuted.Click, _
                                                                                      btnProjector1.Click, _
                                                                                      btnProjector2.Click, _
                                                                                      btnDocumentCameraZoomIn.Click, _
                                                                                      btnDocumentCameraZoomOut.Click, _
                                                                                      rbtnDocumentCameraFreeze.Click, _
                                                                                      rbtnDocumentCameraLandscape.Click, _
                                                                                      rbtnDocumentCameraLightOFF.Click, _
                                                                                      rbtnDocumentCameraLightUpper.Click, _
                                                                                      rbtnDocumentCameraPortrait.Click, _
                                                                                      rbtnDocumentCameraRelease.Click, _
                                                                                      rbtnWhiteBoard.Click, _
                                                                                      rbtnAnnotationAuxAVInput.Click, _
                                                                                      rbtnAnnotationVCPrimary.Click, _
                                                                                      rbtnAnnotationVCSecondary.Click, _
                                                                                      rbtnAnnotationDocumentCamera.Click, _
                                                                                      rbtnAnnotationGuestDevices.Click, _
                                                                                      rbtnAnnotationLecternComputer.Click, _
                                                                                      rbtnAnnotationPresenterCamera.Click, _
                                                                                      btnPresenterCameraDown.Click, _
                                                                                      btnPresenterCameraLeft.Click, _
                                                                                      btnPresenterCameraRight.Click, _
                                                                                      btnPresenterCameraUp.Click, _
                                                                                      btnPresenterCameraZoomTele.Click, _
                                                                                      btnPresenterCameraZoomWide.Click, _
                                                                                      TrbLecternMic.Click, _
                                                                                      TrbWirelessMIC01.Click, _
                                                                                      TrbWirelessMIC02.Click, _
                                                                                      TrbWirelessMIC03.Click, _
                                                                                      TrbWirelessMIC04.Click, _
                                                                                      chbLecternMic.Click, _
                                                                                      chbWirelessMIC01.Click, _
                                                                                      chbWirelessMIC02.Click, _
                                                                                      chbWirelessMIC03.Click, _
                                                                                      chbWirelessMIC04.Click, _
                                                                                      btnScreen1Stop.Click, _
                                                                                      btnScreen2Stop.Click, _
                                                                                      rbtnScreen1Down.Click, _
                                                                                      rbtnScreen1Up.Click, _
                                                                                      rbtnScreen2Down.Click, _
                                                                                      rbtnScreen2Up.Click, _
                                                                                      chbProjector1Power.Click, _
                                                                                      chbProjector2Power.Click, _
                                                                                      rbtnLightBlackOut.Click, _
                                                                                      rbtnLightExtraQuality.Click, _
                                                                                      rbtnLightQualityProjection.Click, _
                                                                                      rbtnLightTeach.Click, _
                                                                                      rbtnLightWelcome.Click, _
                                                                                      chbBoardLights1.Click, _
                                                                                      chbBoardLights2.Click, _
                                                                                      chbBoardLights3.Click, _
                                                                                      chbPresenterSpots1.Click, _
                                                                                      chbPresenterSpots2.Click, _
                                                                                      chbAuxiluarySpots1.Click, _
                                                                                      chbAuxiluarySpots2.Click, _
                                                                                      rbtnOverFlowModeDouble.Click, _
                                                                                      rbtnOverFlowModeSingle.Click, _
                                                                                      rbtnOverFlowReceive.Click, _
                                                                                      rbtnOverFlowSend.Click, _
                                                                                      rbtnOverFlowStandAlone.Click, _
                                                                                      btnCaseRoom1.Click, _
                                                                                      btnCaseRoom2.Click, _
                                                                                      btnCaseRoom3.Click, _
                                                                                      btnCaseRoom4.Click, _
                                                                                      btnOGGB3.Click, _
                                                                                      btnOGGB4.Click, _
                                                                                      btnOGGB5.Click, _
                                                                                      btn098.Click, _
                                                                                      btnFAndP.Click, _
                                                                                      btnOverFlowEngagedDisconnect.Click


        If Not Exam_Mode Or Not Event_Mode Then
            ExitNoTouchTimerReset()
        End If
    End Sub


    Private Sub AnnotationButtonSkinChangeMouseUp(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs)
        'ExitNoTouchTimerReset()
    End Sub

    Private Sub AnnotationButtonSkinChangeMouseDown(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs)
        'ExitNoTouchTimerReset()
    End Sub

    Private Sub ScreenDisplaySkinChange_MouseUp(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles btnScreen2Stop.MouseUp, btnScreen1Stop.MouseUp
        'ExitNoTouchTimerReset()
    End Sub

    Private Sub ScreenDisplaySkinChange_MouseDown(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles btnScreen2Stop.MouseDown, btnScreen1Stop.MouseDown
        'ExitNoTouchTimerReset()
    End Sub

    Private Sub PresenterCameraSkinChange_MouseUp(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles btnPresenterCameraZoomWide.MouseUp, btnPresenterCameraZoomTele.MouseUp, btnPresenterCameraUp.MouseUp, btnPresenterCameraRight.MouseUp, btnPresenterCameraLeft.MouseUp, btnPresenterCameraDown.MouseUp
        'ExitNoTouchTimerReset()
    End Sub

    Private Sub PresenterCameraSkinChange_MouseDown(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles btnPresenterCameraZoomWide.MouseDown, btnPresenterCameraZoomTele.MouseDown, btnPresenterCameraUp.MouseDown, btnPresenterCameraRight.MouseDown, btnPresenterCameraLeft.MouseDown, btnPresenterCameraDown.MouseDown
        'ExitNoTouchTimerReset()
    End Sub

    Private Sub DocumentCameraButtonSkinChangeMouseUp(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles btnDocumentCameraZoomOut.MouseUp, btnDocumentCameraZoomIn.MouseUp
        'ExitNoTouchTimerReset()
    End Sub

    Private Sub btnRoomSettingKeyPad_MouseUp(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles btnRoomSettingKeyPadDot.MouseUp, btnRoomSettingKeyPadBackSpace.MouseUp, btnRoomSettingKeyPad9.MouseUp, btnRoomSettingKeyPad8.MouseUp, btnRoomSettingKeyPad7.MouseUp, btnRoomSettingKeyPad6.MouseUp, btnRoomSettingKeyPad5.MouseUp, btnRoomSettingKeyPad4.MouseUp, btnRoomSettingKeyPad3.MouseUp, btnRoomSettingKeyPad2.MouseUp, btnRoomSettingKeyPad1.MouseUp, btnRoomSettingKeyPad0.MouseUp
        'ExitNoTouchTimerReset()
    End Sub

    Private Sub btnRoomSettingKeyPad_MouseDown(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles btnRoomSettingKeyPadDot.MouseDown, btnRoomSettingKeyPadBackSpace.MouseDown, btnRoomSettingKeyPad9.MouseDown, btnRoomSettingKeyPad8.MouseDown, btnRoomSettingKeyPad7.MouseDown, btnRoomSettingKeyPad6.MouseDown, btnRoomSettingKeyPad5.MouseDown, btnRoomSettingKeyPad4.MouseDown, btnRoomSettingKeyPad3.MouseDown, btnRoomSettingKeyPad2.MouseDown, btnRoomSettingKeyPad1.MouseDown, btnRoomSettingKeyPad0.MouseDown
        'ExitNoTouchTimerReset()
    End Sub

    Private Sub RoomSetting_SkinChange_MouseUp(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles btnRoomSettingSave.MouseUp, btnRoomSettingBack.MouseUp, btnRoomSetting.MouseUp, btnCloseTouchPanel.MouseUp
        'ExitNoTouchTimerReset()
    End Sub

    Private Sub RoomSetting_SkinChange_MouseDown(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles btnRoomSettingSave.MouseDown, btnRoomSettingBack.MouseDown, btnRoomSetting.MouseDown, btnCloseTouchPanel.MouseDown
        'ExitNoTouchTimerReset()
    End Sub

    Private Sub SourcePageButtonSkinChangeMouseUp(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles btnPageUp.MouseUp, btnPageDown.MouseUp
        'ExitNoTouchTimerReset()
    End Sub

    Private Sub SourcePageButtonSkinChangeMouseDown(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles btnPageUp.MouseDown, btnPageDown.MouseDown
        'ExitNoTouchTimerReset()
    End Sub

    Private Sub rbtnSource_MouseClick(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles rbtnSource5.MouseClick, rbtnSource4.MouseClick, rbtnSource3.MouseClick, rbtnSource2.MouseClick, rbtnSource1.MouseClick
        'ExitNoTouchTimerReset()
    End Sub

    Private Sub btnPresenterCameraLeft_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles btnPresenterCameraLeft.MouseMove

    End Sub
End Class
