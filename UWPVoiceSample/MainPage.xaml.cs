using Azure.Communication;
using Azure.Communication.Calling;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWPVoiceSample
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
            var callAgentOptions = new CallAgentOptions()
            {
                DisplayName = "ACS Voice User"
            };
            callAgent = await callClient.CreateCallAgent(token_credential, callAgentOptions);
        }

        // 通話開始ボタン押下イベント
        private async void CallButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            StartCallOptions startCallOptions = new StartCallOptions();
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
    }
}
