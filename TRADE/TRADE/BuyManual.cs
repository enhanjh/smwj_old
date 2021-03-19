using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;


namespace SMWJ
{
    public class BuyManual
    {
        private Form1 _formObj;

        public Timer _timer;

        //public string _accnt;

        public BuyManual(Form1 formObj)
        {
            this._formObj = formObj;
        }


        // 타이머
        private static DateTime Delay(int MS)
        {
            DateTime ThisMoment = DateTime.Now;
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, MS);
            DateTime AfterWards = ThisMoment.Add(duration);

            while (AfterWards >= ThisMoment)
            {
                System.Windows.Forms.Application.DoEvents();
                ThisMoment = DateTime.Now;
            }

            return DateTime.Now;
        }


        public void BuyManualStarter()
        {
            // 타이머 생성 및 시작
            _timer = new System.Timers.Timer();
            _timer.Interval = 1 * 1000; // 1초
            _timer.Elapsed += new ElapsedEventHandler(TimerElapsed);
            _timer.Start();
        }


        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            string strHhminss = DateTime.Now.ToString("HHmmss");
            int hhminss = int.Parse(strHhminss);
            string today = DateTime.Now.ToString("yyyyMMdd");

            _formObj.ShowThreadState(strHhminss, "AB");

            // 추매 등 수동 매수 요청 조회
            if (hhminss >= 90000 && hhminss < 153000)
            {
                RetBuyManualOrder(today);
            }
        }


        // 추매 등 수동 매수 요청 내역 조회
        private void RetBuyManualOrder(string today)
        {
            string query = "";
            query += " SELECT A.MODE";
            query += "      , A.ITEM";
            query += "      , A.CNT";
            query += "      , A.PRICE";
            query += "      , A.ACCOUNT";
            query += "   FROM BUY_REQ A";
            query += "  WHERE A.REQ_DAY = '" + today + "'";
            query += "    AND A.REQ_SP  = 1";

            ArrayList buyReqList = _formObj._db.SelectQuery(query);

            if (buyReqList.Count > 0)
            {
                ReqBuyManual(buyReqList);
            }
        }


        // 지정가 매수 요청(mode가 99가 아니고, 가격이 없을 경우 시장가 매수)
        public void ReqBuyManual(ArrayList arr)
        {
            foreach (Dictionary<string, string> row in arr)
            {
                // 과부하 막기 위한 슬립
                Delay(Constants.SLEEP_TIME);

                // 거래구분 취득
                // 00:지정가, 03:시장가, 05:조건부지정가, 06:최유리지정가, 07:최우선지정가,
                // 10:지정가IOC, 13:시장가IOC, 16:최유리IOC, 20:지정가FOK, 23:시장가FOK,
                // 26:최유리FOK, 61:장개시전시간외, 62:시간외단일가매매, 81:시간외종가
                string orderCond = "00";

                string mode = row["MODE"];
                int price   = int.Parse(row["PRICE"]);
                
                if (price == 0 && mode != "99")
                {
                    orderCond = "03";
                }

                // 매매구분 취득
                // 1:신규매수, 2:신규매도 3:매수취소, 4:매도취소, 5:매수정정, 6:매도정정
                int trType = 1;

                string acctNo = Constants.ACCT;
                
                string item = row["ITEM"];
                int amt = int.Parse(row["CNT"]);
                
                _formObj.Logger(Log.매수, "[지정가 매수 요청] : 종목 : {0}, 수량 : {1}, 가격 : {2}", item, amt, price);

                // 주식주문
                _formObj.OpenApiOrder(
                      Constants.REQ_BUY_ADD     // 화면번호
                    , acctNo                    // 계좌번호
                    , trType                    // 매매구분
                    , item                      // 종목코드
                    , amt                       // 주문수량
                    , price                     // 주문가격
                    , orderCond                 // 거래구분
                    , ""                        // 원주문번호
                    );

                string today = DateTime.Now.ToString("yyyyMMdd");
                _formObj._buy.SaveBuyReq(today, item, mode);

                SaveBuyManualReq(today, item, mode, amt, price);
            }
        }


        // 추매 요청 상태를 요청으로 변경
        public void SaveBuyManualReq(string today, string item, string mode, int amt, int price)
        {
            string query = "";
            query = "";
            query += " UPDATE BUY_REQ";
            query += "    SET REQ_SP = 2";
            query += "  WHERE REQ_DAY = '" + today + "'";
            query += "    AND REQ_SP  = 1";
            query += "    AND ITEM    = '" + item + "'";
            query += "    AND MODE    = " + mode;

            _formObj._db.InsertQuery(query);

            _formObj.Logger(Log.매수, "[추매 요청 업데이트] : 종목 : {0}, 일자 : {1}, 수량 : {2}, 가격 : {3}", item, today, amt, price);
        }


        // 매수 완료 후, 추매 종목 존재 여부 확인
        public void RetAdditionalBuyCnt(string today, string item, int totalPrice, int todayFee)
        {
            _formObj.Logger(Log.매수, "[추매 내역 업데이트] : 종목 : {0}, 일자 : {1}", item, today);

            // 추매 주문 수량이 모두 체결되어야 추매 관련 내역을 업데이트 함
            string query = "";
            query += " SELECT A.ITEM";
            query += "      , SUM(A.BUY_CNT) AS CNT";
            query += "      , SUM(A.BUY_TOT) AS TOT";
            query += "      , SUM(A.BUY_FEE) AS FEE";
            query += "      , SUM(A.BUY_AMT) AS AMT";
            query += "   FROM TRAN A";
            query += "  WHERE A.TRAN_DAY = '" + today + "'";
            query += "    AND A.ITEM     = '" + item + "'";
            query += "    AND A.TRAN_SP  = 2";
            query += "  GROUP BY A.ITEM";
            query += " HAVING COUNT(*) = 2";

            ArrayList boughtList = _formObj._db.SelectQuery(query);

            if (boughtList.Count > 0)
            {
                Dictionary<string, string> row = (Dictionary<string, string>)boughtList[0];

                // 1
                // 기존 매수의 sell_order_no 를 99로 변경
                // sell_order_no 가 99인 항목은 정산에 포함하지 않음
                query  = "";
                query += " UPDATE TRAN";
                query += "    SET SELL_ORDER_NO = '99'";
                query += "      , TIME_2        = NOW()";
                query += "  WHERE TRAN_DAY      = '" + today + "'";
                query += "    AND ITEM          = '" + row["ITEM"] + "'";
                query += "    AND TRAN_SP       = 2";
                query += "    AND MODE         != 99";

                _formObj._db.InsertQuery(query);

                // 2
                // 추매의 buy_cnt, buy_tot, buy_fee, buy_amt를 기존 매수와 합산
                // buy_price 는 buy_tot 를 buy_cnt 로 나누어 계산 
                query  = "";
                query += " UPDATE TRAN";
                query += "    SET BUY_PRICE    = FLOOR('" + row["TOT"] + "' / '" + row["CNT"] + "')";
                query += "      , BUY_CNT      = '" + row["CNT"] + "'";
                query += "      , BUY_TOT      = '" + row["TOT"] + "'";
                query += "      , BUY_FEE      = '" + row["FEE"] + "'";
                query += "      , BUY_AMT      = '" + row["AMT"] + "'";
                query += "      , TIME_2       = NOW()";
                query += "  WHERE TRAN_DAY  = '" + today + "'";
                query += "    AND ITEM      = '" + row["ITEM"] + "'";
                query += "    AND TRAN_SP   = 2";
                query += "    AND MODE      = 99";

                _formObj._db.InsertQuery(query);

                int dealAmt = totalPrice + todayFee;

                // 3
                // 추매에 사용한 예산을 추매예산 테이블에서 차감함
                //query  = "";
                //query += " INSERT INTO ACCNT_ADD";
                //query += " (ACCOUNT, DEAL_SP, DEAL_AMT, ACTIVATE_DAY, WORK_MAN, WORK_TIME)";
                //query += " VALUES";
                //query += " ('" + _accnt + "', '2', " + dealAmt + ", '" + today + "', 'P', NOW())";
                
                //_formObj._db.InsertQuery(query);

                // 추매 예산 차감 후에는, 변수를 초기화함
                //_accnt = "";

                // 4
                // 추매 요청 상태를 완료로 변경
                query  = "";
                query += " UPDATE BUY_REQ";
                query += "    SET REQ_SP = 3";
                query += "  WHERE REQ_DAY = '" + today + "'";
                query += "    AND REQ_SP  = 2";
                query += "    AND ITEM    = '" + row["ITEM"] + "'";

                _formObj._db.InsertQuery(query);
            }
        }
    }
}
