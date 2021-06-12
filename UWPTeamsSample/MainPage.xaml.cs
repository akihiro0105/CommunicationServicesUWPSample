using Azure.Communication;
using Azure.Communication.Calling;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWPTeamsSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // アクセストークン
        string user_token_ = "<access token>";

        CallClient callClient;
        CallAgent callAgent;
        Call call;
        DeviceManager deviceManager;
        LocalVideoStream[] localVideoStream;

        public MainPage()
        {
            this.InitializeComponent();

            InitCallAgent();
        }

        // アクセストークンから接続用ユーザーを作成
        private async void InitCallAgent()
        {
            var token_credential = new CommunicationTokenCredential(user_token_);

            callClient = new CallClient();
            deviceManager = await callClient.GetDeviceManager();
            localVideoStream = new LocalVideoStream[1];

            var callAgentOptions = new CallAgentOptions()
            {
                DisplayName = "ACS Teams User"
            };
            callAgent = await callClient.CreateCallAgent(token_credential, callAgentOptions);
            callAgent.OnCallsUpdated += CallAgent_OnCallsUpdated;
            callAgent.OnIncomingCall += CallAgent_OnIncomingCall;
        }

        // 通話着信時のイベント
        private async void CallAgent_OnIncomingCall(object sender, IncomingCall incomingCall)
        {
            GetCameraDevice();

            AcceptCallOptions acceptCallOptions = new AcceptCallOptions();
            acceptCallOptions.VideoOptions = new VideoOptions(localVideoStream);
            call = await incomingCall.AcceptAsync(acceptCallOptions);
        }

        // 相手ビデオ受信時のイベント
        private async void CallAgent_OnCallsUpdated(object sender, CallsUpdatedEventArgs args)
        {
            foreach (var call in args.AddedCalls)
            {
                foreach (var remoteParticipant in call.RemoteParticipants)
                {
                    await AddVideoStreams(remoteParticipant.VideoStreams);
                    remoteParticipant.OnVideoStreamsUpdated += async (s, a) => await AddVideoStreams(a.AddedRemoteVideoStreams);
                }
                call.OnRemoteParticipantsUpdated += Call_OnRemoteParticipantsUpdated; ;
                call.OnStateChanged += Call_OnStateChanged;
            }
        }

        // 通話状態変更イベント
        private async void Call_OnStateChanged(object sender, PropertyChangedEventArgs args)
        {
            switch (((Call)sender).State)
            {
                // 通話終了
                case CallState.Disconnected:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        LocalVideo.Source = null;
                        RemoteVideo = null;
                    });
                    break;
                default:
                    break;
            }
        }

        private async void Call_OnRemoteParticipantsUpdated(object sender, ParticipantsUpdatedEventArgs args)
        {
            foreach (var remoteParticipant in args.AddedParticipants)
            {
                await AddVideoStreams(remoteParticipant.VideoStreams);
                remoteParticipant.OnVideoStreamsUpdated += async (s, a) => await AddVideoStreams(a.AddedRemoteVideoStreams);
            }
        }

        // 相手ビデオ表示
        private async Task AddVideoStreams(IReadOnlyList<RemoteVideoStream> streams)
        {

            foreach (var remoteVideoStream in streams)
            {
                var remoteUri = await remoteVideoStream.CreateBindingAsync();

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    RemoteVideo.Source = remoteUri;
                    RemoteVideo.Play();
                    RemoteVideo.Width = 750;
                });
                remoteVideoStream.Start();
            }
        }

        // 通話開始ボタン押下イベント
        private async void CallButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            GetCameraDevice();

            StartCallOptions startCallOptions = new StartCallOptions();
            startCallOptions.VideoOptions = new VideoOptions(localVideoStream);
            ICommunicationIdentifier[] callees = new ICommunicationIdentifier[1]
            {
                new CommunicationUserIdentifier(CallTextBox.Text)
            };
            call = await callAgent.StartCallAsync(callees, startCallOptions);
        }

        // 通話終了ボタン押下イベント
        private async void HangupButton_Click(object sender, RoutedEventArgs e)
        {
            await call.HangUpAsync(new HangUpOptions());
        }

        // 利用するカメラの取得
        private async void GetCameraDevice()
        {
            if (deviceManager.Cameras.Count > 0)
            {
                VideoDeviceInfo videoDeviceInfo = deviceManager.Cameras[0];
                localVideoStream[0] = new LocalVideoStream(videoDeviceInfo);

                Uri localUri = await localVideoStream[0].CreateBindingAsync();

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    LocalVideo.Source = localUri;
                    LocalVideo.Play();
                    LocalVideo.Width = 750;
                });
            }
        }

        // Teams参加ボタン押下イベント
        private async void TeamsButton_Click(object sender, RoutedEventArgs e)
        {
            GetCameraDevice();

            JoinCallOptions joinCallOptions = new JoinCallOptions();
            joinCallOptions.VideoOptions = new VideoOptions(localVideoStream);
            TeamsMeetingLinkLocator teamsMeetingLinkLocator = new TeamsMeetingLinkLocator(CallTextBox.Text);
            call = await callAgent.JoinAsync(teamsMeetingLinkLocator, joinCallOptions);
        }
    }
}
