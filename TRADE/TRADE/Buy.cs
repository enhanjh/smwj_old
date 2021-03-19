using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;


namespace SMWJ
{
    public class Buy
    {
        private Form1 _formObj;      // form object
        public int _buyReqFinishS;    // 매수 종료 여부 flag(시가)
        public int _buyReqFinishE;    // 매수 종료 여부 flag(종가)

        public Timer _timer;

        public Buy(Form1 formObj)
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


        public void BuyStarter()
        {
            // 타이머 생성 및 시작
            _timer = new System.Timers.Timer();
            _timer.Interval = 10 * 1000; // 10초
            _timer.Elapsed += new ElapsedEventHandler(TimerElapsed);
            _timer.Start();
        }


        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            int hhminss = int.Parse(DateTime.Now.ToString("HHmmss"));
            string today = DateTime.Now.ToString("yyyyMMdd");

            _formObj.ShowThreadState(hhminss.ToString(), "B");

            if (hhminss >= 90000 && hhminss <= 91000)
            {
                if (_buyReqFinishS != 1)
                {                    
                    // ITEM : 이미 매수 요청한 종목은 제외함, 과거 매수한 종목 중 청산하지 못한 종목은 제외함
                    // TRAN_DAY : 어제 저장한 풀 종목 조회
                    // BUY_INVOLVE_ITEM_YN : 전략별 매수 필요 조건을 만족하지 못한 종목은 제외
                    // CNT : 매수 수량이 0 이상인 종목만 조회
                    string query = "";
                    query += " SELECT A.MODE";
                    query += "      , A.ITEM";
                    query += "      , A.PRICE";
                    query += "      , A.CNT";
                    query += "   FROM POOL A";
                    query += "  WHERE A.ITEM NOT IN (SELECT AA.ITEM";
                    query += "                         FROM TRAN AA";
                    query += "                        WHERE AA.TRAN_DAY = '" + today + "'";
                    query += "                      )";
                    query += "    AND A.TRAN_DAY = (SELECT MAX(BB.TRAN_DAY)";
                    query += "                        FROM POOL BB";
                    query += "                       WHERE BB.TRAN_DAY < '" + today + "'";
                    query += "                     )";
                    query += "    AND A.BUY_INVOLVE_ITEM_YN = 'Y'";
                    query += "    AND A.CNT > 0";
                    query += "    AND A.MODE IN (1, 2, 6, 7)";

                    ArrayList pool = _formObj._db.SelectQuery(query);
                    
                    if (pool != null && pool.Count > 0)
                    {
                        ReqBuy(pool);
                    }
                    else
                    {
                        _buyReqFinishS = 1;
                    } 
                }
            }
            // 종가 매수(쌍매수)
            else if (hhminss > 151300 && hhminss <= 152000)
            {
                if (_buyReqFinishE != 1)
                {
                    // ITEM : 이미 매수 요청한 종목은 제외함, 과거 매수한 종목 중 청산하지 못한 종목은 제외함
                    // TRAN_DAY : 오늘 저장한 풀 종목 조회
                    // BUY_INVOLVE_ITEM_YN : 관리종목 제외, 이동평균 조건 충족 종목 조회
                    // CNT : 매수 수량이 0 이상인 종목만 조회
                    string query = "";
                    query += " SELECT A.MODE";
                    query += "      , A.ITEM";
                    query += "      , A.PRICE";
                    query += "      , A.CNT";
                    query += "   FROM POOL A";
                    query += "  WHERE A.ITEM NOT IN (SELECT AA.ITEM";
                    query += "                         FROM TRAN AA";
                    query += "                        WHERE AA.TRAN_DAY = '" + today + "'";
                    query += "                      )";
                    query += "    AND A.TRAN_DAY            = '" + today + "'";
                    query += "    AND A.BUY_INVOLVE_ITEM_YN = 'Y'";
                    query += "    AND A.CNT                 > 0";
                    query += "    AND A.MODE                = 4";

                    ArrayList pool = _formObj._db.SelectQuery(query);

                    if (pool != null && pool.Count > 0)
                    {
                        ReqBuy(pool);
                    }
                    else
                    {
                        _buyReqFinishE = 1;
                    }
                }
            }
        }


        // 시장가 매수 요청
        public void ReqBuy(ArrayList arr)
        {
            foreach (Dictionary<string, string> row in arr)
            {
                // 과부하 막기 위한 슬립
                Delay(Constants.SLEEP_TIME);

                // 거래구분 취득
                // 00:지정가, 03:시장가, 05:조건부지정가, 06:최유리지정가, 07:최우선지정가,
                // 10:지정가IOC, 13:시장가IOC, 16:최유리IOC, 20:지정가FOK, 23:시장가FOK,
                // 26:최유리FOK, 61:장개시전시간외, 62:시간외단일가매매, 81:시간외종가
                string orderCond = "03";
                
                // 매매구분 취득
                // 1:신규매수, 2:신규매도 3:매수취소, 4:매도취소, 5:매수정정, 6:매도정정
                int trType = 1;

                string acctNo = Constants.ACCT;
                string mode = row["MODE"];
                string item = row["ITEM"];
                int amt = int.Parse(row["CNT"]);
                int price = int.Parse("0");
                    
                _formObj.Logger(Log.매수, "[매수 요청] : 종목 : {0}, 수량 : {1}, 가격 : {2}", item, amt, price);

                // 주식주문
                _formObj.OpenApiOrder(
                      Constants.REQ_BUY  // 화면번호
                    , acctNo             // 계좌번호
                    , trType             // 매매구분
                    , item               // 종목코드
                    , amt                // 주문수량
                    , price              // 주문가격
                    , orderCond          // 거래구분
                    , ""                 // 원주문번호
                    );

                string today = DateTime.Now.ToString("yyyyMMdd");
                SaveBuyReq(today, item, mode);
            }
        }


        // 매수 요청 내역 DB 저장
        public void SaveBuyReq(string today, string item, string mode)
        {
            _formObj.Logger(Log.매수, "[매수 요청 내역 DB 저장] : 종목 : {0}, 일자 : {1}", item, today);

            // 주문 내역 DB저장 
            string query = "";
            query += " INSERT INTO TRAN";
            query += " (TRAN_DAY, TRAN_ID, MODE, ITEM, TRAN_SP, TIME_1, ADJUST_YN)";
            query += " VALUES";
            query += " ('" + today + "', '" + today + "-" + item + "', '" + mode + "', '" + item + "', 1, NOW(), 'N')";

            _formObj._db.InsertQuery(query);
        }


        // 매수 요청 체결 내역 업데이트
        public void ReqBuy_Callback(string orderNo, double avgPrice, int orderAmt, int remainAmt, int totalPrice, int todayFee, string today, string item)
        {
            _formObj.Logger(Log.매수, "[매수 요청 체결 내역 업데이트] : 종목 : {0}, 일자 : {1}, 평단가 : {2}, 미체결 : {3}", item, today, avgPrice, remainAmt);

            string query = "";
            query += " UPDATE TRAN";
            query += "    SET BUY_ORDER_NO = '" + orderNo + "'";
            query += "      , BUY_PRICE    = '" + avgPrice + "'";
            query += "      , BUY_CNT      = '" + (remainAmt == 0 ? orderAmt : (orderAmt - remainAmt)) + "'";
            query += "      , BUY_TOT      = '" + totalPrice + "'";
            query += "      , BUY_FEE      = '" + todayFee + "'";
            query += "      , BUY_AMT      = '" + (remainAmt == 0 ? (totalPrice + todayFee) : 0 ) + "'";
            query += "      , TRAN_SP      = '" + (remainAmt == 0 ? "2" : "1") + "'";
            query += "      , TIME_2       = " + (remainAmt == 0 ? "NOW()" : "NULL");
            query += "  WHERE TRAN_DAY  = '" + today + "'";
            query += "    AND TRAN_ID   = '" + today + "-" + item + "'";
            query += "    AND ITEM      = '" + item + "'";
            query += "    AND TRAN_SP   = 1";

            _formObj._db.InsertQuery(query);
        }
    }
}
