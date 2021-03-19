using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using log4net;
using log4net.Config;

namespace SMWJ
{
    public partial class Form1 : Form
    {
        // 각 모듈에서 DB 사용하기 위한 변수
        public DatabaseTask _db;

        // 장 개시 여부 저장 변수
        public bool _operTime = false;

        // 각 모듈에 접근하기 위한 변수
        public Buy _buy;
        public Sell _sell;
        public BuyManual _buyManual;

        // form initialize
        public Form1()
        {
            InitializeComponent();

            // log4net 시작
            XmlConfigurator.Configure(new System.IO.FileInfo("log4net.xml"));

            // database connection create
            _db = new DatabaseTask();
        }


        // 타이머
        private static DateTime Delay(int MS)
        {
            DateTime thisMoment = DateTime.Now;
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, MS);
            DateTime afterWards = thisMoment.Add(duration);

            while (afterWards >= thisMoment)
            {
                System.Windows.Forms.Application.DoEvents();
                thisMoment = DateTime.Now;
            }

            return DateTime.Now;
        }


        // 로그 출력
        public void Logger(Log type, string format, params Object[] args)
        {
            ILog logger = LogManager.GetLogger("smwjLogger");

            string hhmin = "[" + DateTime.Now.ToString("HHmm") + "]";
            string message = string.Format(format, args);

            logger.Info(type.ToString() + "-" + message);
        }


        // 폼 로딩 완료 후 로그인 창 생성
        private void Form1_Load(object sender, EventArgs e)
        {
            if (axKHOpenAPI.CommConnect() == 0)
            {
                Logger(Log.일반, "[로그인창 열기] : 성공");
                lblLogin.Text = "창 열기 성공";
            }
            else
            {
                Logger(Log.에러, "[로그인창 열기] : 실패");
                lblLogin.Text = "창 열기 실패";
            }
        }


        // 폼 종료시 모든 연결 종료
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            axKHOpenAPI.CommTerminate();
            Logger(Log.일반, "[로그아웃]");
        }


        // 로그인 결과 판단
        private void axKHOpenAPI_OnEventConnect(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnEventConnectEvent e)
        {
            if (e.nErrCode == 0)
            {
                Logger(Log.일반, "[로그인 결과] : 성공");
                lblLogin.Text = "성공";

                _buy = new Buy(this);
                _sell = new Sell(this);
                _buyManual = new BuyManual(this);

                // 타이머시작
                ReqThreadStart();
            }
            else
            {
                Logger(Log.에러, "[로그인 결과] : 실패");
                lblLogin.Text = "실패";
            }
        }


        // 로그인 정보 조회
        public Dictionary<string, string> OpenApiLoginInfo()
        {
            string userId = axKHOpenAPI.GetLoginInfo("USER_ID");
            string userNm = axKHOpenAPI.GetLoginInfo("USER_NAME");
            string[] acctNo = axKHOpenAPI.GetLoginInfo("ACCNO").Split(';');

            Dictionary<string, string> rslt = new Dictionary<string, string>();

            rslt.Add("userId", userId);
            rslt.Add("userNm", userNm);
            for (int i = 0; i < acctNo.Length; i++)
            {
                rslt.Add("acctNo" + i.ToString(), acctNo[i]);
            }

            return rslt;
        }


        // 로그아웃
        private void btnLogout_Click(object sender, EventArgs e)
        {
            axKHOpenAPI.CommTerminate();
            Logger(Log.일반, "[로그아웃]");
        }

        
        // 접속 상태 확인
        public Boolean OpenApiGetConnStatus()
        {
            Boolean rslt = false;
            if (axKHOpenAPI.GetConnectState() == 0)
            {
                Logger(Log.일반, "[Open API 연결] : 미연결");
            }
            else
            {
                Logger(Log.일반, "[Open API 연결] : 연결됨");
                rslt = true;
            }

            return rslt;
        }


        // 실시간 연결 종료
        private void OpenApiDisconnectAllRealData(string scrNo)
        {
            axKHOpenAPI.DisconnectRealData(scrNo);
        }


        // 실시간 정보 수신할 종목 등록
        public void OpenApiRegisterRealItem(string sScrNo, ArrayList items, string sFid)
        {
            string item = "";
            for (int i = 0; i < items.Count; i++)
            {
                string temp = (string)items[i];

                item = item + temp + ";";
            }

            // sRealType - 0:한 번에 등록, 1:여러번에 걸쳐 등록
            axKHOpenAPI.SetRealReg(sScrNo, item, sFid, "0");
        }


        // 실시간 정보 수신한 종목 해제
        public void OpenApiUnregisterRealItem(string sScrNo, string item)
        {
            axKHOpenAPI.SetRealRemove(sScrNo, item);
        }


        // Open API에 tr데이터 요청
        public void OpenApiRequest(string reqName, string reqCode, int type, string scrNum, Dictionary<string, string> param)
        {
            axKHOpenAPI.SetRealRemove("ALL", "ALL");

            // 과부하 막기 위한 슬립
            Delay(Constants.SLEEP_TIME);

            foreach (KeyValuePair<string, string> temp in param)
            {
                axKHOpenAPI.SetInputValue(temp.Key, temp.Value);
            }

            int nRet = axKHOpenAPI.CommRqData(reqName, reqCode, type, scrNum);

            if (Error.IsError(nRet))
            {                
                ShowTrMsg("요청 성공");
                //Logger(Log.일반, "[TR 요청 {0}] : " + Error.GetErrorMessage(), reqCode);
            }
            else
            {
                ShowTrMsg("요청 실패");
                Logger(Log.에러, "[TR 요청 {0}] : " + Error.GetErrorMessage(), reqCode);
            }            
        }


        // Open API 주식 주문
        public void OpenApiOrder(string scrNum, string acctNo, int trType, string item, int amt, int price, string orderCond, string orgOrderNo) {

            int lRet = axKHOpenAPI.SendOrder("주식주문", scrNum, acctNo, trType, item, amt, price, orderCond, orgOrderNo);

            string today = DateTime.Now.ToString("yyyyMMdd");

            if (lRet == 0)
            {
                //Logger(Log.일반, "[주문 요청] 주문이 전송 되었습니다.");
            }
            else
            {
                Logger(Log.에러, "[주문 요청] 주문이 전송 실패 하였습니다. [에러] : " + lRet);
            }
        }

        
        // Open API 체결 잔고 수신
        private void axKHOpenAPI_OnReceiveChejanData(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveChejanDataEvent e)
        {
            Logger(Log.체결잔고, "====체결잔고 수신 시작====");
            
            if (e.sGubun == "0")
            {
                string acctNo  = axKHOpenAPI.GetChejanData(9201).Trim(); // 계좌번호
                string orderNo = axKHOpenAPI.GetChejanData(9203).Trim(); // 주문번호
                string trType  = axKHOpenAPI.GetChejanData(907).Trim(); // 매수도구분(1":매도, 2":매수)

                string item   = axKHOpenAPI.GetChejanData(9001).Trim(); // 종목코드
                string itemNm = axKHOpenAPI.GetChejanData(302).Trim(); // 종목명
                
                string buyPrice = axKHOpenAPI.GetChejanData(910).Trim(); // 체결가                
                string buyAmt   = axKHOpenAPI.GetChejanData(911).Trim(); // 체결량
                
                int orderAmt   = int.Parse(axKHOpenAPI.GetChejanData(900).Trim()); // 주문수량
                int remainAmt  = int.Parse(axKHOpenAPI.GetChejanData(902).Trim()); // 미체결수량
                int totalPrice = int.Parse(axKHOpenAPI.GetChejanData(903).Trim()); // 체결누계금액
                
                int todayFee = int.Parse(axKHOpenAPI.GetChejanData(938).Trim()); // 당일매매 수수료
                int todayTax = int.Parse(axKHOpenAPI.GetChejanData(939).Trim()); // 당일매매 세금

                int avgAmt = orderAmt - remainAmt;
                double avgPrice = 0;
                if (avgAmt != 0)
                {
                    avgPrice = totalPrice / avgAmt;
                }
                avgPrice = Math.Floor(avgPrice);

                int type = Convert.ToInt32(trType);

                Logger(Log.체결잔고, "구분 : {6} 주문체결통보, 주문체결시간 : {0}, 종목명 : {1}, 주문수량 : {2}, 주문가격 : {3}, 체결수량 : {4}, 체결가격 : {5}"
                    , axKHOpenAPI.GetChejanData(908), itemNm, orderAmt, axKHOpenAPI.GetChejanData(901), buyAmt, buyPrice, (type==1?"매도":"매수"));
                
                string today = DateTime.Now.ToString("yyyyMMdd");

                // 체결가격이 없으면 업데이트하지 않음
                if (totalPrice > 0)
                {
                    // 매도 주문체결 잔고 확인시
                    if (type == 1)
                    {
                        Logger(Log.매도, "[매도 체잔 수신] 종목 : " + item + ", 체결누계금액 : " + totalPrice + ", 주문수량 : " + orderAmt + ", 미체결수량 : " + remainAmt + ", 평단가 : " + avgPrice);
                        _sell.ReqSell_Callback(orderNo, avgPrice, orderAmt, remainAmt, totalPrice, todayFee, todayTax, today, item.Substring(1)); // 종목코드에 영문자가 붙어서 나옴...
                    }
                    // 매수 주문체결 잔고 확인시
                    else if (type == 2)
                    {
                        Logger(Log.매수, "[매수 체잔 수신] 종목 : " + item + ", 체결누계금액 : " + totalPrice + ", 주문수량 : " + orderAmt + ", 미체결수량 : " + remainAmt + ", 평단가 : " + avgPrice);
                        _buy.ReqBuy_Callback(orderNo, avgPrice, orderAmt, remainAmt, totalPrice, todayFee, today, item.Substring(1)); // 종목코드에 영문자가 붙어서 나옴...

                        _buyManual.RetAdditionalBuyCnt(today, item.Substring(1), totalPrice, todayFee);
                    }
                }
            }
            else if (e.sGubun == "1")
            {
                Logger(Log.체결잔고, "구분 : 잔고통보");
            }
            else if (e.sGubun == "3")
            {
                Logger(Log.체결잔고, "구분 : 특이신호");
            }

            Logger(Log.체결잔고, "====체결잔고 수신 끝====");
        }


        // open API tr데이터 수신 이벤트
        private void axKHOpenAPI_OnReceiveTrData(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            axKHOpenAPI.SetRealRemove("ALL", "ALL");

            ShowTrMsg("수신 시작");
            Logger(Log.조회, "====TR 수신 시작====");
            Logger(Log.조회, "화면번호 :{0} | RQName :{1} | TRCode :{2} | 레코드명 :{3} | 연속조회 유무 :{4}", e.sScrNo, e.sRQName, e.sTrCode, e.sRecordName, e.sPrevNext);

            string todayFull = System.DateTime.Now.ToString("yyyyMMddHHmm");
            string today = todayFull.Substring(0, 8);
            string nowHhmm = todayFull.Substring(8);
            //DateTime ydate = DateTime.Now - TimeSpan.FromDays(1);
            
            // OPT10085 : 계좌수익률요청
            if ("OPT10085".Equals(e.sTrCode))
            {
                // 매도할 잔고 종목 조회할 때
                if ((Constants.RET_ACCT_ITEM).Equals(e.sScrNo.Substring(0, 4)))
                {
                    int nCnt = axKHOpenAPI.GetRepeatCnt(e.sTrCode, e.sRQName);

                    ArrayList rslt = new ArrayList();
                    for (int i = 0; i < nCnt; i++)
                    {
                        string item     = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "종목코드").Trim();
                        string tranDay  = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "일자").Trim();
                        string amt      = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "보유수량").Trim();
                        string price    = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "현재가").Trim();
                        string buyPrice = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "매입가").Trim();
                        string buyTot   = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "매입금액").Trim();

                        Dictionary<string, string> row = new Dictionary<string, string>();
                        row.Add("item"    , item    );
                        row.Add("tranDay" , tranDay );
                        row.Add("amt"     , amt     );
                        row.Add("price"   , price   );
                        row.Add("buyPrice", buyPrice);
                        row.Add("buyTot"  , buyTot  );

                        rslt.Add(row);
                    }

                    _sell.RetAcctItem_Callback(rslt, today);
                }
            }

            Logger(Log.조회, "====TR 수신 끝====");
            ShowTrMsg("수신 끝");
        }


        // Open API 실시간데이터 수신 이벤트
        private void axKHOpenAPI_OnReceiveRealData(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveRealDataEvent e)
        {
            string item = e.sRealKey;

            Logger(Log.실시간, "====실시간 수신 시작====");
            Logger(Log.실시간, "종목코드 : {0} | RealType : {1} | RealData : {2}", item, e.sRealType, e.sRealData);

            if( "09".Equals( item ) && "장시작시간".Equals( e.sRealType ) ) {
                string realData = e.sRealData;
                if( realData.Contains("085900") ) {
                    //string today = System.DateTime.Now.ToString("yyyyMMdd") + "085900";
                    //DateTime sysDate = DateTime.ParseExact(today, "yyyyMMddHHmmss", null);
                    //DateTime now = DateTime.Now;
                    //TimeSpan span = sysDate.Subtract(now);
                    //DateTime.Now.Add(span);
                    // 장 개시가 확실할 경우, 값 변경
                    _operTime = true;
                    Logger(Log.실시간, "[시간변경] : " + DateTime.Now.ToString());
                }
            }

            Logger(Log.실시간, "====실시간 수신 끝====");
        }


        // Open API 메세지 수신 이벤트
        private void axKHOpenAPI_OnReceiveMsg(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveMsgEvent e)
        {
            Logger(Log.조회, "====메세지 수신 시작====");
            Logger(Log.조회, "화면번호 : {0} | RQName : {1} | TRCode : {2}", e.sScrNo, e.sRQName, e.sTrCode);
            Logger(Log.조회, "====메세지 수신 끝====");
        }


        // 프로그램 종료
        public void ReqAppStop()
        {
            Application.Exit();
        }

        
        // 스레드종료 버튼 클릭하여 종료
        private void ReqThreadStop()
        {
            _buy._timer.Stop();
            _sell._timer.Stop();
            _buyManual._timer.Stop();
        }


        // 스레드시작
        private void ReqThreadStart()
        {
            if (_operTime == true)
            {
                // buy thread start            
                Thread buyTh = new Thread(_buy.BuyStarter);
                buyTh.Start();

                // sell thread start            
                Thread sellTh = new Thread(_sell.SellStarter);
                sellTh.Start();

                // addBuy thread start
                Thread addBuyTh = new Thread(_buyManual.BuyManualStarter);
                addBuyTh.Start();

                MessageBox.Show("스레드시작 성공!");
            }
        }


        // TR조회 횟수 표시
        public void ShowTrCount(int now, int all)
        {
            ShowTrMsg(now.ToString() + " / " + all.ToString());
        }


        // TR메시지 표시
        public void ShowTrMsg(string msg)
        {
            if (lblTr.InvokeRequired)
            {
                lblTr.BeginInvoke(new Action(delegate()
                {
                    lblTr.Text = msg;
                }));
            }
            else
            {
                lblTr.Text = msg;
            }
        }


        // 스레드 표시
        public void ShowThreadState(string msg, string cls)
        {
            if (cls == "B")
            {
                label5.BeginInvoke(new Action(delegate()
                {
                    label5.Text = msg;
                }));
                
            }
            else if (cls == "S")
            {
                label6.BeginInvoke(new Action(delegate()
                {
                    label6.Text = msg;
                }));

            }
            else if (cls == "AB")
            {
                label8.BeginInvoke(new Action(delegate()
                {
                    label8.Text = msg;
                }));

            }
        }


        // 장 개시 이후, 프로그램 구동시, 타이머를 가동하기위해 변수 조정
        private void button1_Click(object sender, EventArgs e)
        {
            _operTime = true;

            // 타이머시작
            ReqThreadStart();
        }
    }
}