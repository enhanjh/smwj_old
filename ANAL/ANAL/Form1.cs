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
        // 영업일 여부
        public bool _bizDayYn;

        // 각 모듈에서 DB 사용하기 위한 변수
        public DatabaseTask _db;

        // 각 모듈에 접근하기 위한 변수
        public Strategy _sttg;
        public ApiTask _api;

        // OPT10080 에서 연속조회할 때 사용하는 전역변수
        public string _priceTime;
        public int _yyyymmddCnt = 0;

        // form initialize
        public Form1()
        {
            InitializeComponent();

            // log4net 시작
            XmlConfigurator.Configure(new System.IO.FileInfo("log4net.xml"));

            // database connection create
            _db = new DatabaseTask();

            // apitask 객체 생성
            _api = new ApiTask(this);

            // 초기변수 설정
            _bizDayYn = false;
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
            // 요청구분
            for (int i = 0; i < 12; i++)
            {
                cmbReq.Items.Add(KOACode.reqGb[i].name);
            }

            cmbReq.SelectedIndex = 0;

            if (_api.ApiLogin())
            {
                Logger(Log.일반, "[로그인 요청 결과] : 성공");
                lblLogin.Text = "성공";
            }
            else
            {
                Logger(Log.에러, "[로그인 요청 결과] : 실패");
                lblLogin.Text = "실패";
            }
        }


        // 폼 종료시 모든 연결 종료
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _api.ApiLogout();
            Logger(Log.일반, "[로그아웃]");
        }


        // 접속 상태 확인
        public void ApiGetConnStatus()
        {
            if (_api.ApiGetConnStatus())
            {
                Logger(Log.일반, "[Open API 연결] : 연결됨"); 
            }
            else
            {
                Logger(Log.일반, "[Open API 연결] : 미연결");
            }
        }



        /*
        // open API tr데이터 수신 이벤트
        private void axKHOpenAPI_OnReceiveTrData(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            //axKHOpenAPI.SetRealRemove("ALL", "ALL");

            ShowTrMsg("수신 시작");
            Logger(Log.조회, "====TR 수신 시작====");
            Logger(Log.조회, "화면번호 :{0} | RQName :{1} | TRCode :{2} | 레코드명 :{3} | 연속조회 유무 :{4}", e.sScrNo, e.sRQName, e.sTrCode, e.sRecordName, e.sPrevNext);

            string todayFull = System.DateTime.Now.ToString("yyyyMMddHHmm");
            string today = todayFull.Substring(0, 8);
            string nowHhmm = todayFull.Substring(8);
            //DateTime ydate = DateTime.Now - TimeSpan.FromDays(1);

            // OPT10001 : 주식기본정보 요청
            if (e.sTrCode == "OPT10001")
            {
                string item = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "종목코드").Trim();
                if ("".Equals(item) || item == null)
                {
                    Logger(Log.조회, "종목코드 부재");
                }
                else
                {
                    string itemNm         = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "종목명").Trim();
                    int startPrice        = Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "시가").Trim());
                    int endPrice          = Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "현재가").Trim());
                    int highPrice         = Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "고가").Trim());
                    int lowPrice          = Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "저가").Trim());
                    int stckAmt           = Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "상장주식").Trim());
                    string marketCap      = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "시가총액").Trim();
                    string marketCapRatio = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "시가총액비중").Trim();
                    string per            = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "PER").Trim();
                    string eps            = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "EPS").Trim();
                    string roe            = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "ROE").Trim();
                    string pbr            = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "PBR").Trim();
                    string revenue        = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "매출액").Trim();
                    string ebit           = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "영업이익").Trim();
                    string netIncome      = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "당기순이익").Trim();
                    int highest           = Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "250최고").Trim());
                    int lowest            = Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "250최저").Trim());
                    string highestDt      = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "250최고가일").Trim();
                    string lowestDt       = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "250최저가일").Trim();
                    string diffSign       = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "대비기호").Trim();
                    int diff              = Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "전일대비").Trim());
                    string rate           = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "등락율").Trim();
                    int tranAmt           = Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "거래량").Trim());
                    string tranDiff       = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, 0, "거래대비").Trim();

                    Dictionary<string, object> param = new Dictionary<string, object>();
                    param.Add("item", item);
                    param.Add("itemNm", itemNm);
                    param.Add("startPrice", startPrice);
                    param.Add("endPrice", endPrice);
                    param.Add("highPrice", highPrice);
                    param.Add("lowPrice", lowPrice);
                    param.Add("stckAmt", stckAmt);
                    param.Add("marketCap", marketCap);
                    param.Add("marketCapRatio", marketCapRatio);
                    param.Add("per", per);
                    param.Add("eps", eps);
                    param.Add("roe", roe);
                    param.Add("pbr", pbr);
                    param.Add("revenue", revenue);
                    param.Add("ebit", ebit);
                    param.Add("netIncome", netIncome);
                    param.Add("highest", highest);
                    param.Add("lowest", lowest);
                    param.Add("highestDt", highestDt);
                    param.Add("lowestDt", lowestDt);
                    param.Add("diffSign", diffSign);
                    param.Add("diff", diff);
                    param.Add("rate", rate);
                    param.Add("tranAmt", tranAmt);
                    param.Add("tranDiff", tranDiff);

                    Logger(Log.전략, "{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}",
                            stckAmt,
                            marketCap,
                            marketCapRatio,
                            per,
                            eps,
                            roe,
                            pbr,
                            revenue,
                            ebit,
                            netIncome,
                            highest,
                            highestDt,
                            lowest,
                            lowestDt,
                            diffSign,
                            diff,
                            rate,
                            tranAmt,
                            tranDiff                        
                        );

                    // 종목 마스터 조회할 때
                    if ((Constants.RET_ITEM_MST).Equals(e.sScrNo.Substring(0, 4)))
                    {
                        param["itemSp"] = e.sRQName.Trim();

                        string construction = axKHOpenAPI.GetMasterConstruction(item);
                        param["caution"] = "N";
                        param["warn"]    = "N";
                        param["danger"]  = "N";

                        string state = axKHOpenAPI.GetMasterStockState(item);
                        param["state"] = state.Substring(0,20);

                        if ("정상".Equals(construction))
                        {

                        }
                        else if ("투자주의".Equals(construction))
                        {
                            param["caution"] = "Y";
                        }
                        else if ("투자경고".Equals(construction))
                        {
                            param["warn"] = "Y";
                        }
                        else if ("투자위험".Equals(construction))
                        {
                            param["danger"] = "Y";
                        }

                        _sttg.RetRefreshItemMst_Callback(today, param);
                    }
                    else if ((Constants.RET_GI).Equals(e.sScrNo.Substring(0, 4)))
                    {
                        Logger(Log.전략, "{0}, {1} | 시가 :{2}, 현재가 :{3}, 고가 :{4}, 저가 :{5}, 상장주식 :{6}, 시가총액 :{7}",
                            item,
                            itemNm,
                            startPrice,
                            endPrice,
                            highPrice,
                            lowPrice,
                            stckAmt,
                            marketCap
                        );

                        lblCurPrice.Text = endPrice.ToString();
                    }
                }
            }
            // OPT10004 : 주식호가요청
            if (e.sTrCode == "OPT10004")
            {
                int nCnt = axKHOpenAPI.GetRepeatCnt(e.sTrCode, e.sRQName);

                for (int i = 0; i < nCnt; i++)
                {
                    int price = Math.Abs(Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "매수최우선호가").Trim()));

                    // 매수 비율 판단전 시초가 조회할 때
                    if ((Constants.RET_NEAR_START_PRC).Equals(e.sScrNo.Substring(0, 4)))
                    {
                        string[] temp = ((e.sRQName).Trim()).Split(new Char[] { '-' });
                        string item     = temp[0];
                        string mode     = temp[1];
                        string yyyymmdd = temp[2];
                        _sttg.RetBeginningPrice_Callback(item, price, mode, yyyymmdd);

                        _sttg._beginningPriceFlag = true;
                    }
                }
            }
            // OPT10017 : 상하한가요청
            else if (e.sTrCode == "OPT10017")
            {
                int nCnt = axKHOpenAPI.GetRepeatCnt(e.sTrCode, e.sRQName);

                ArrayList rslt = new ArrayList();

                string[] temp = (e.sRQName).Trim().Split(new Char[] { '-' });
                string tranDay = temp[1];

                for (int i = 0; i < nCnt; i++)
                {
                    string item = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "종목코드").Trim();
                    int price = Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "현재가").Trim());
                    double ratio = double.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "등락률").Trim());
                    int diff = int.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "전일대비").Trim());

                    Logger(Log.전략, "{0},{1} | 현재가 :{2:N0} | 등락율 :{3} | 거래량 :{4:N0} ",
                        item,
                        axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "종목명").Trim(),
                        price,
                        ratio,
                        Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "거래량").Trim())
                    );

                    if ((Constants.RET_ULP).Equals(e.sScrNo.Substring(0, 4)))
                    {
                        Dictionary<string, string> row = new Dictionary<string, string>();

                        row.Add("item", item);
                        row.Add("price", price.ToString());
                        row.Add("rate", ratio.ToString());
                        row.Add("tranDay", tranDay);
                        row.Add("mode", "1");

                        if (ratio > 0)
                        {
                            rslt.Add(row);
                        }
                    }
                    else if ((Constants.RET_RECENT_ULP).Equals(e.sScrNo.Substring(0, 4)))
                    {
                        RetRecentUpper_Callback(item, price, ratio);
                    }
                    else if ((Constants.RET_ALL_ULP).Equals(e.sScrNo.Substring(0, 4)))
                    {
                        _sttg.RetAllUlp_Callback(today, item, price, diff, ratio);
                    }
                }

                if (rslt.Count > 0 && (Constants.RET_ULP).Equals(e.sScrNo.Substring(0, 4)))
                {
                    _sttg.RetPoolItem1_Callback(rslt);
                }
            }
            // OPT10023 : 거래량급증요청
            else if (e.sTrCode == "OPT10023")
            {
                int nCnt = axKHOpenAPI.GetRepeatCnt(e.sTrCode, e.sRQName);

                string item = "";
                int price = 0;
                int condCnt = 0;

                for (int i = 0; i < nCnt; i++)
                {
                    if (condCnt > 0)
                    {
                        break;
                    }

                    if (i > 10)
                    {
                        break;
                    }

                    string tempItem = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "종목코드").Trim();
                    int tempPrice = Math.Abs(Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "현재가").Trim()));
                    int tempAmt = Math.Abs(Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "현재거래량").Trim()));
                    int tempOldAmt = Math.Abs(Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "이전거래량").Trim()));

                    int tempTot = tempPrice * (tempAmt - tempOldAmt);

                    Logger(Log.전략, "종목 : {0}, 현재가 : {1}, 거래금액 : {2}, 거래량 : {3} / {4}, ", tempItem, tempPrice, tempTot, tempAmt, tempOldAmt);
                    // 1. 새롭게 급등한 종목
                    // 2. 거래금액 약 2억원 초과
                    if (!((_sttg._tranAmtSpikeItem).Contains(tempItem)) && tempTot > 200000000)
                    {
                        item = tempItem;
                        price = tempPrice;
                        condCnt++;
                    }
                }

                if (item.Length > 0 && price > 0)
                {
                    // ETF등 복합주식은 매수하지 않음
                    if (item.StartsWith("0") || item.StartsWith("1"))
                    {
                        _sttg.RetTranAmtSpike_Callback(item, price, nowHhmm);
                    }
                }
            }
            // OPT10059 : 종목별투자자기관별요청
            else if (e.sTrCode == "OPT10059")
            {
                if (e.sScrNo.Substring(0, 4) == Constants.RET_ITEM_INVST)
                {
                    int nCnt = axKHOpenAPI.GetRepeatCnt(e.sTrCode, e.sRQName);

                    string[] temp = (e.sRQName).Trim().Split(new Char[] { '-' });
                    string item    = temp[0];
                    string tranDay = temp[1];

                    ArrayList rslt = new ArrayList();

                    for (int i = 0; i < nCnt; i++)
                    {
                        string ymd = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "일자").Trim();
                        int amt    = int.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "누적거래대금").Trim());
                        int ant    = int.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "개인투자자").Trim());
                        int fore   = int.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "외국인투자자").Trim());
                        int ins    = int.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "기관계").Trim());

                        Logger(Log.전략, "종목 : {0}, 일자 : {1}, {5}, 개인 : {2}, 외국인 : {3}, 기관 : {4}",
                            item,
                            ymd,
                            ant,
                            fore,
                            ins,
                            tranDay
                        );

                        Dictionary<string, object> param = new Dictionary<string, object>();
                        param.Add("item", item);
                        param.Add("ymd", ymd);
                        param.Add("amt", amt);
                        param.Add("ant", ant);
                        param.Add("fore", fore);
                        param.Add("ins", ins);

                        if (ymd == tranDay)
                        {
                            rslt.Add(param);
                            _sttg.RetInvestor_Callback(rslt);

                            break;
                        }
                    }

                    _sttg._investorFlag = true;
                }
                else if (e.sScrNo.Substring(0, 4) == Constants.RET_ITEM_INVST_L)
                {
                    int nCnt = axKHOpenAPI.GetRepeatCnt(e.sTrCode, e.sRQName);

                    string[] temp = (e.sRQName).Trim().Split(new Char[] { '-' });
                    string item = temp[0];
                    string tranDay = temp[1];

                    ArrayList rslt = new ArrayList();

                    for (int i = 0; i < nCnt; i++)
                    {
                        string ymd = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "일자").Trim();
                        int amt = int.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "누적거래대금").Trim());
                        int ant = int.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "개인투자자").Trim());
                        int fore = int.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "외국인투자자").Trim());
                        int ins = int.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "기관계").Trim());

                        Logger(Log.전략, "종목 : {0}, 일자 : {1}, {5}, 개인 : {2}, 외국인 : {3}, 기관 : {4}",
                            item,
                            ymd,
                            ant,
                            fore,
                            ins,
                            tranDay
                        );

                        Dictionary<string, object> param = new Dictionary<string, object>();
                        param.Add("item", item);
                        param.Add("ymd", ymd);
                        param.Add("amt", amt);
                        param.Add("ant", ant);
                        param.Add("fore", fore);
                        param.Add("ins", ins);

                        rslt.Add(param);

                        if (ymd == "20150701")
                        {
                            break;
                        }
                    }

                    _sttg.RetInvestor_Callback(rslt);

                    _sttg._investorFlag = true;
                }
            }
            // OPT10062 : 동일순매매순위요청
            else if (e.sTrCode == "OPT10062")
            {
                if (e.sScrNo.Substring(0, 4) == Constants.RET_DUAL_PULLING_S)
                {
                    int nCnt = axKHOpenAPI.GetRepeatCnt(e.sTrCode, e.sRQName);

                    string[] temp = (e.sRQName).Trim().Split(new Char[] { '-' });
                    string tranDay = temp[1];

                    ArrayList rslt = new ArrayList();

                    for (int i = 0; i < nCnt; i++)
                    {
                        string item = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "종목코드").Trim();
                        int price = Math.Abs(int.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "현재가").Trim()));
                        int amt = Math.Abs(int.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "누적거래량").Trim()));
                        double ratio = double.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "등락율").Trim());

                        Logger(Log.전략, "{0},{1} | 현재가 :{2:N0} | 등락율 :{3}",
                            item,
                            axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "종목명").Trim(),
                            price,
                            ratio
                        );

                        Dictionary<string, string> row = new Dictionary<string, string>();

                        row.Add("item", item);
                        row.Add("price", price.ToString());
                        row.Add("rate", ratio.ToString());
                        row.Add("tranDay", tranDay);
                        row.Add("mode", "2");

                        if (ratio > 0)
                        {
                            rslt.Add(row);
                        }
                    }

                    _sttg.RetPoolItem2_Callback(rslt);
                }
            }
            // OPT10065 : 장중투자자별매매상위요청
            else if (e.sTrCode == "OPT10065")
            {
                if (e.sScrNo.Substring(0, 4) == Constants.RET_DUAL_PULLING_E)
                {
                    int nCnt = axKHOpenAPI.GetRepeatCnt(e.sTrCode, e.sRQName);

                    string[] temp = (e.sRQName).Trim().Split(new Char[] { '-' });
                    string reqCd = temp[1];

                    ArrayList rslt = new ArrayList();

                    for (int i = 0; i < nCnt; i++)
                    {
                        string item = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "종목코드").Trim();

                        rslt.Add(item);
                    }

                    if (reqCd == "9100")
                    {
                        _sttg.RetPoolItem4_1_Callback(rslt); // 외인 매수 조회 콜백
                    }
                    else if (reqCd == "9999")
                    {
                        _sttg.RetPoolItem4_2_Callback(rslt, today); // 기관 매수 조회 콜백
                    }
                }
            }
            // OPT10007 : 시세표성정보
            else if (e.sTrCode == "OPT10007")
            {
                if (e.sScrNo.Substring(0, 4) == Constants.COMP_TRAN_AMT)
                {
                    int nCnt = axKHOpenAPI.GetRepeatCnt(e.sTrCode, e.sRQName);

                    string[] temp = (e.sRQName).Trim().Split(new char[] { ';' });
                    string item = temp[0];
                    string price = temp[1];
                    string ratio = temp[2];
                    string tranDay = temp[3];
                    string mode = temp[4];
                    int ydayAmt = 0;
                    int todayAmt = 0;
                    int todayTot = 0;
                    int startPrc = 0;

                    for (int i = 0; i < nCnt; i++)
                    {
                        string yyyymmdd = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "날짜").Trim();

                        todayAmt = Math.Abs(int.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "거래량").Trim()));
                        string tempTot = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "거래대금").Trim();
                        if (tempTot.Length <= 0)
                        {
                            todayTot = 0;
                        }
                        else
                        {
                            todayTot = Math.Abs(int.Parse(tempTot));
                        }
                        startPrc = Math.Abs(int.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "시가").Trim()));

                        if (price == "0")
                        {
                            price = (Math.Abs(int.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "현재가").Trim()))).ToString();
                        }

                        if (ratio == "99")
                        {
                            ratio = (Math.Abs(double.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "등락률").Trim()))).ToString();
                        }

                        ydayAmt = Math.Abs(int.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "전일거래량").Trim()));
                    }

                    Logger(Log.전략, "종목코드 :{0} | 현재가 :{1} | 등락율 :{2} | 당일거래량 : {3} | 전일거래량 : {4} | 당일거래대금 : {5}",
                        item,
                        price,
                        ratio,
                        todayAmt,
                        ydayAmt,
                        todayTot
                    );

                    // 1. 거래량 전일비 2배
                    // 2. 거래대금 50억 이상
                    // *. 이격도 조건은 매수 수량 계산 시점에서 확인함
                    // *. 등락율 조건은 쌍매수 조회 후 확인했음(쌍매수(시가))
                    // *. 당일 시가 < 당일 종가(쌍매수(종가))
                    if (todayAmt > (ydayAmt * 2) && (todayTot > 5000))
                    {
                        if (mode == "4")
                        {
                            if (int.Parse(price) > startPrc)
                            {
                                _sttg.CompTranAmt_Callback(item, price, ratio, tranDay, mode);
                            }
                        }
                        else
                        {
                            _sttg.CompTranAmt_Callback(item, price, ratio, tranDay, mode);
                        }
                    }

                    // 쌍매수(종가) 전체 저장
                    if (mode == "4")
                    {
                        string query = "";
                        query += " INSERT INTO POOL";
                        query += " (MODE, TRAN_DAY, ITEM, PRICE, RATE, POOL_TIME)";
                        query += " VALUES";
                        query += " ( 44, '" + tranDay + "', '" + item + "', '" + price + "', '" + ratio + "', NOW())";
                        query += " ON DUPLICATE KEY UPDATE PRICE     = '" + price + "'";
                        query += "                       , RATE      = '" + ratio + "'";
                        query += "                       , POOL_TIME = NOW()";

                        _db.InsertQuery(query);
                    }

                    _sttg._compareTranAmtFlag = true;
                }
            }
            // OPT10080 : 주식분봉차트조회요청
            else if ("OPT10080".Equals(e.sTrCode))
            {
                // 풀추적 조회할 때
                if ((Constants.RET_POOL_TRACKING).Equals(e.sScrNo.Substring(0, 4)))
                {
                    int nCnt = axKHOpenAPI.GetRepeatCnt(e.sTrCode, e.sRQName);

                    string date = txtReqDt.Text.Trim();
                    if (date == "" || date == null)
                    {
                        date = today;
                    }

                    string[] temp = (e.sRQName).Trim().Split(new Char[] { '-' });
                    string yyyymmdd = temp[0];
                    string item = temp[1];
                    string mode = temp[2];

                    string hhmin = "";

                    int highPrice = 0;
                    string highHhmin = "";
                    int lowPrice = 10000000;
                    string lowHhmin = "";
                    int startPrice = 0;
                    int endPrice = 0;
                    int ydayPrice = 0;

                    // 연속조회의 콜백일 때
                    if (_priceTime != null && _priceTime.Length > 0)
                    {
                        string[] tmp = _priceTime.Trim().Split(new Char[] { ';' });
                        highPrice = int.Parse(tmp[0]);
                        highHhmin = tmp[1];
                        lowPrice = int.Parse(tmp[2]);
                        lowHhmin = tmp[3];
                        endPrice = int.Parse(tmp[4]);
                    }


                    for (int i = 0; i < nCnt; i++)
                    {
                        hhmin = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "체결시간").Trim();

                        if (Int32.Parse(hhmin.Substring(0, 8)) > Int32.Parse(date))
                        {
                            continue;
                        }
                        else if (hhmin.Substring(0, 8) == date)
                        {
                            int tempHighPrice = Math.Abs(Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "고가").Trim()));
                            int tempLowPrice = Math.Abs(Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "저가").Trim()));

                            if (highPrice <= tempHighPrice)
                            {
                                highPrice = tempHighPrice;
                                highHhmin = hhmin;
                            }

                            if (lowPrice >= tempLowPrice)
                            {
                                lowPrice = tempLowPrice;
                                lowHhmin = hhmin;
                            }

                            if (_yyyymmddCnt == 0)
                            {
                                endPrice = Math.Abs(Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "현재가").Trim()));
                            }

                            startPrice = Math.Abs(Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "시가").Trim()));

                            _yyyymmddCnt++;
                        }
                        else if (Int32.Parse(hhmin.Substring(0, 8)) < Int32.Parse(date))
                        {
                            ydayPrice = Math.Abs(Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "현재가").Trim()));

                            break;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (hhmin == "" || hhmin == null)
                    {
                        hhmin = "19770101";
                    }

                    Logger(Log.전략, "종목코드 : {0}, 고가 : {1}, 고가시간 : {2}, 저가 : {3}, 저가시간 : {4}, 시가 : {5}, 종가 : {6}, 최종시간 : {7}", item, highPrice, highHhmin, lowPrice, lowHhmin, startPrice, endPrice, hhmin);

                    if (e.sPrevNext == "0")
                    {
                        _sttg.RetPoolTracking_Callback(date, startPrice, endPrice, highPrice, highHhmin, lowPrice, lowHhmin, item, mode, ydayPrice);

                        _priceTime = "";
                        _yyyymmddCnt = 0;

                        _sttg._poolTrackingFlag = true;
                    }
                    else
                    {
                        if (Int32.Parse(hhmin.Substring(0, 8)) >= Int32.Parse(date))
                        {
                            _priceTime = highPrice.ToString() + ";" + highHhmin + ";" + lowPrice.ToString() + ";" + lowHhmin + ";" + endPrice.ToString();

                            Dictionary<string, string> param = new Dictionary<string, string>();
                            param.Add("종목코드", item);
                            param.Add("틱범위", "1"); // 틱범위 1분
                            param.Add("수정주가구분", "0"); // 수정주가아님

                            Logger(Log.전략, "[풀 추적 연속 조회 시작] 종목코드 : {0}", param["종목코드"]);

                            OpenApiRequest(yyyymmdd + "-" + param["종목코드"] + "-" + mode, "OPT10080", 2, e.sScrNo, param);
                        }
                        else
                        {
                            _sttg.RetPoolTracking_Callback(date, startPrice, endPrice, highPrice, highHhmin, lowPrice, lowHhmin, item, mode, ydayPrice);

                            _priceTime = "";
                            _yyyymmddCnt = 0;

                            _sttg._poolTrackingFlag = true;
                        }
                    }
                }
                // 거래량 급등 종목 매수 고려할 때
                else if ((Constants.RET_TRAN_AMT_SPIKE).Equals(e.sScrNo.Substring(0, 4)))
                {
                    int nCnt = axKHOpenAPI.GetRepeatCnt(e.sTrCode, e.sRQName);

                    string[] temp = ((e.sRQName).Trim()).Split(new Char[] { ';' });
                    string item = temp[0];
                    string hhmm = temp[1];

                    double curPrice = 0;
                    double curTranAmt = 0;
                    double prevPrice = 0;
                    double prevTranAmt = 0;
                    double prevPrevTranAmt = 0;
                    int curHhmin = 0;
                    int prevHhmin = 0;
                    double oldPrice = 0;
                    double curBeginPrice = 0;

                    // 가장 최근의 정보가 배열 첫 번째
                    for (int i = 0; i < nCnt; i++)
                    {
                        if (i == 0)
                        {
                            curPrice = Math.Abs(double.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "현재가").Trim()));
                            curTranAmt = Math.Abs(double.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "거래량").Trim()));
                            curHhmin = Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "체결시간").Trim().Substring(8, 4));

                            curBeginPrice = Math.Abs(double.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "시가").Trim()));
                        }
                        else if (i == 1)
                        {
                            prevPrice = Math.Abs(double.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "현재가").Trim()));
                            prevTranAmt = Math.Abs(double.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "거래량").Trim()));
                            prevHhmin = Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "체결시간").Trim().Substring(8, 4));
                        }
                        else if (i == 2)
                        {
                            prevPrevTranAmt = Math.Abs(double.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "거래량").Trim()));
                        }
                        else if (i > 2 && i < 5)
                        {
                            continue;
                        }
                        else
                        {
                            oldPrice = Math.Abs(double.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "현재가").Trim()));
                            break;
                        }
                    }

                    Logger(Log.전략, "[매수 고려][{4}] : 현가 : {0}, 현시가 : {8}, 이가 : {1}, 올가 : {7}, 현거량 : {2}, 이거량 : {3}, 현시 : {5}, 이시 : {6}", curPrice, prevPrice, curTranAmt, prevTranAmt, e.sRQName, curHhmin, prevHhmin, oldPrice, curBeginPrice);

                    if ((curHhmin - prevHhmin) == 1)
                    {
                        if (curPrice != 0 && prevPrice != 0)
                        {
                            // 1. 현재가와 1분전 종가의 등락률 0.1% 초과 1.5% 이하
                            // 2. 6분전 종가와 5분전 종가의 등락률 0.1% 초과 1% 이하
                            // 3. 현재 시가가 전 종가 이상
                            // 4. 거래량 증가율 3배 이상 7배 이하
                            // 5. 이전거래량이 그 이전거래량 보다 1.5배 이하
                            // *. 변동폭이 높을 것(나중에 추가할 것)
                            double priceRate = curPrice / prevPrice;
                            double tranAmtRate = curTranAmt / prevTranAmt;
                            double prevTranAmtRate = prevTranAmt / prevPrevTranAmt;
                            double oldPriceRate = curPrice / oldPrice;

                            if (priceRate > 1.001 && priceRate <= 1.015 && oldPriceRate > 1.001 && oldPriceRate <= 1.01 && curBeginPrice >= prevPrice)
                            {
                                if (tranAmtRate >= 3 && tranAmtRate <= 7 && prevTranAmtRate <= 1.5)
                                {
                                    string query = "";
                                    query += " SELECT COUNT(*) AS CNT";
                                    query += "   FROM TRAN A";
                                    query += "  WHERE A.TRAN_DAY = '" + today + "'";
                                    query += "    AND A.ITEM     = '" + item + "'";
                                    query += "    AND A.TRAN_SP  = 2";

                                    ArrayList cnt = _db.SelectQuery(query);
                                    Dictionary<string, string> alreadyBought = (Dictionary<string, string>)cnt[0];
                                    int itemCnt = int.Parse(alreadyBought["CNT"]);

                                    double balance = Math.Floor((double)(_sttg._balance * Constants.BUY_RATIO_3 / 100));

                                    Logger(Log.전략, "[거래량 급등 종목 매수 직전][{5}] : 예수금 잔고: {0}, 종목 : {1}, 현재가 : {2}, 등락률 : {3:F2}, 거래량증가율 : {4:F2}, 기매수종목 : {5}", balance, item, curPrice, priceRate, tranAmtRate, e.sRQName, itemCnt);

                                    // 오늘 기준, 매도 하지 못한 종목들 중 사려는 종목이 없는 경우에만 매수
                                    if (itemCnt < 1)
                                    {
                                        int tranAmt = Convert.ToInt32(Math.Floor((double)(balance / curPrice)));

                                        if (tranAmt > 0)
                                        {
                                            query = "";

                                            query += " INSERT INTO BUY_REQ";
                                            query += " (REQ_DAY, MODE, ITEM, REQ_SP, CNT, PRICE, WORK_TIME)";
                                            query += " VALUES";
                                            query += " ('" + today + "', 3, '" + item + "', '1', " + tranAmt + ", 0, NOW())";

                                            _db.InsertQuery(query);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // OPT10081 : 주식일봉차트조회요청
            else if ("OPT10081".Equals(e.sTrCode))
            {
                // 이동평균 및 이격도 조회할 때(당일)
                if ((Constants.RET_AVG_DSP).Equals(e.sScrNo.Substring(0, 4)))
                {
                    int nCnt = axKHOpenAPI.GetRepeatCnt(e.sTrCode, e.sRQName);

                    string[] temp = (e.sRQName).Trim().Split(new Char[] { '-' });
                    string item = temp[0];
                    string yyyymmdd = temp[1];

                    int price = 0; // 종가
                    int beginPrice = 0; // 시가
                    int highPrice = 0; // 고가
                    int lowPrice = 0; // 저가

                    double tranTot = 0; // 거래대금

                    double avgFive = 0; // 5일 이동평균
                    double avgTen = 0; // 10일 이동평균
                    double avgTwenty = 0; // 20일 이동평균
                    double avgSixty = 0; // 60일 이동평균
                    double avgNinety = 0; // 90일 이동평균
                    double avgHunTwen = 0; // 120일 이동평균

                    double dspFive = 0; // 5일 이격도
                    double dspTen = 0; // 10일 이격도
                    double dspTwenty = 0; // 20일 이격도
                    double dspSixty = 0; // 60일 이격도
                    double dspNinety = 0; // 90일 이격도
                    double dspHunTwen = 0; // 120일 이격도

                    double bolTop = 0; // 볼린저밴드 상단값(avg20 + 2 stddev)
                    double bolBtm = 0; // 볼린저밴드 하단값(avg20 - 2 stddev)

                    double tempSum = 0;
                    List<double> endPriceArr = new List<double>();

                    for (int i = 0; i < nCnt; i++)
                    {
                        int endPrice = Math.Abs(Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "현재가").Trim()));

                        endPriceArr.Add(endPrice);
                        tempSum += endPrice;

                        // 현재가
                        if (i == 0)
                        {
                            price = endPrice;
                            beginPrice = Math.Abs(Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "시가").Trim()));
                            highPrice = Math.Abs(Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "고가").Trim()));
                            lowPrice = Math.Abs(Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "저가").Trim()));
                            tranTot = Math.Abs(Double.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "거래대금").Trim()));
                        }
                        // 5일 이동평균
                        else if (i == 4)
                        {
                            avgFive = Math.Round((double)(tempSum / 5), 0);
                        }
                        // 10일 이동평균
                        else if (i == 9)
                        {
                            avgTen = Math.Round((double)(tempSum / 10), 0);
                        }
                        // 20일 이동평균
                        else if (i == 19)
                        {                            
                            avgTwenty = Math.Round((double)(tempSum / 20), 0);

                            bolTop = avgTwenty + Math.Round((2 * Stat.stdDev(endPriceArr, 20)), 2);
                            bolBtm = avgTwenty - Math.Round((2 * Stat.stdDev(endPriceArr, 20)), 2);
                        }
                        // 60일 이동평균
                        else if (i == 59)
                        {
                            avgSixty = Math.Round((double)(tempSum / 60), 0);
                        }
                        // 90일 이동평균
                        else if (i == 89)
                        {
                            avgNinety = Math.Round((double)(tempSum / 90), 0);
                        }
                        // 120일 이동평균
                        else if (i == 119)
                        {
                            avgHunTwen = Math.Round((double)(tempSum / 120), 0);
                        }
                        else if (i >= 120)
                        {
                            break;
                        }
                    }

                    dspFive = Math.Round((double)(price / avgFive * 100), 2);
                    dspTen = Math.Round((double)(price / avgTen * 100), 2);
                    dspTwenty = Math.Round((double)(price / avgTwenty * 100), 2);
                    dspSixty = Math.Round((double)(price / avgSixty * 100), 2);
                    dspNinety = Math.Round((double)(price / avgNinety * 100), 2);
                    dspHunTwen = Math.Round((double)(price / avgHunTwen * 100), 2);

                    Logger(Log.전략, "종목코드 : {0}, 현재가 : {1}, 5일 이평 : {2}, 5일 이격 : {3}, 10일 이평 : {4}, 10일 이격 : {5}, 20일 이평 : {6}, 20일 이격 : {7}",
                        item, price, avgFive, dspFive, avgTen, dspTen, avgTwenty, avgTwenty);

                    Dictionary<string, object> param = new Dictionary<string, object>();
                    param.Add("item", item);
                    param.Add("yyyymmdd", yyyymmdd);
                    param.Add("dspFive", dspFive);
                    param.Add("dspTen", dspTen);
                    param.Add("dspTwenty", dspTwenty);
                    param.Add("dspSixty", dspSixty);
                    param.Add("dspNinety", dspNinety);
                    param.Add("dspHunTwen", dspHunTwen);
                    param.Add("avgFive", avgFive);
                    param.Add("avgTen", avgTen);
                    param.Add("avgTwenty", avgTwenty);
                    param.Add("avgSixty", avgSixty);
                    param.Add("avgNinety", avgNinety);
                    param.Add("avgHunTwen", avgHunTwen);
                    param.Add("price", price);
                    param.Add("beginPrice", beginPrice);
                    param.Add("highPrice", highPrice);
                    param.Add("lowPrice", lowPrice);
                    param.Add("bolTop", bolTop);
                    param.Add("bolBtm", bolBtm);
                    param.Add("tranTot", tranTot);

                    _sttg.RetAverageDisparity_Callback(param);

                    _sttg._poolAvgDspFlag = true;
                }
                // 이동평균 및 이격도 조회할 때(장기)
                else if ((Constants.RET_AVG_DSP_L).Equals(e.sScrNo.Substring(0, 4)))
                {
                    int nCnt = axKHOpenAPI.GetRepeatCnt(e.sTrCode, e.sRQName);

                    string[] temp = (e.sRQName).Trim().Split(new Char[] { '-' });
                    string item = temp[0];
                    string yyyymmdd = temp[1];

                    int endPrice = 0; // 종가
                    int beginPrice = 0; // 시가
                    int highPrice = 0; // 고가
                    int lowPrice = 0; // 저가

                    double tranTot = 0; // 거래대금

                    ArrayList rslt = new ArrayList();

                    for (int i = 0; i < nCnt; i++)
                    {
                        
                        endPrice = Math.Abs(Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "현재가").Trim()));
                        beginPrice = Math.Abs(Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "시가").Trim()));
                        highPrice = Math.Abs(Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "고가").Trim()));
                        lowPrice = Math.Abs(Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "저가").Trim()));
                        tranTot = Math.Abs(Double.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "거래대금").Trim()));
                        yyyymmdd = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "일자").Trim();

                        Logger(Log.전략, "종목코드 : {0}, 현재가 : {1}, 일자 : {2}", item, endPrice, yyyymmdd);

                        Dictionary<string, object> param = new Dictionary<string, object>();
                        param.Add("item", item);
                        param.Add("yyyymmdd", yyyymmdd);
                        param.Add("price", endPrice);
                        param.Add("beginPrice", beginPrice);
                        param.Add("highPrice", highPrice);
                        param.Add("lowPrice", lowPrice);
                        param.Add("tranTot", tranTot);

                        rslt.Add(param);

                        if (yyyymmdd == "20161212")
                        {
                            break;
                        }
                    }                    

                    _sttg.RetAverageDisparityLong_Callback(rslt);

                    _sttg._poolAvgDspFlag = true;
                }
            }
            // OPTKWFID : 관심종목정보요청
            else if ("OPTKWFID".Equals(e.sTrCode))
            {
                // 관심종목 조회할 때
                if ((Constants.RET_INTEREST_ITEM).Equals(e.sScrNo.Substring(0, 4)))
                {
                    int nCnt = axKHOpenAPI.GetRepeatCnt(e.sTrCode, e.sRQName);

                    ArrayList rslt = new ArrayList();
                    for (int i = 0; i < nCnt; i++)
                    {
                        string item = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "종목코드").Trim();
                        string itemNm = axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "종목명").Trim();
                        int price = Math.Abs(Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "현재가").Trim()));
                        int highPrice = Math.Abs(Int32.Parse(axKHOpenAPI.CommGetData(e.sTrCode, "", e.sRQName, i, "고가").Trim()));

                        Dictionary<string, object> row = new Dictionary<string, object>();
                        row.Add("item", item);
                        row.Add("itemNm", itemNm);
                        row.Add("price", price);
                        row.Add("highPrice", highPrice);

                        rslt.Add(row);

                        Logger(Log.조회, "종목코드 :{0} | 종목명 :{1} | 현재가 :{2} | 고가 :{3}", item, itemNm, price, highPrice);
                    }

                    _sttg.RetInterestItem_Callback(rslt, today);
                }
            }

            Logger(Log.조회, "====TR 수신 끝====");
            ShowTrMsg("수신 끝");
        }
        */

        /*
        // Open API 실시간데이터 수신 이벤트
        private void axKHOpenAPI_OnReceiveRealData(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveRealDataEvent e)
        {
            string item = e.sRealKey;

            Logger(Log.실시간, "====실시간 수신 시작====");
            Logger(Log.실시간, "종목코드 : {0} | RealType : {1} | RealData : {2}", item, e.sRealType, e.sRealData);

            if( "09".Equals( item ) && "장시작시간".Equals( e.sRealType ) ) {
                string realData = e.sRealData;
                if( realData.Length > 0 && _bizDayYn == false) 
                {
                    Logger(Log.실시간, "[영업일 확정] : " + DateTime.Now.ToString());

                    _bizDayYn = true;
                }
                /*
                if( realData.Contains("085900") ) {
                    string today = System.DateTime.Now.ToString("yyyyMMdd") + "085900";
                    DateTime sysDate = DateTime.ParseExact(today, "yyyyMMddHHmmss", null);
                    DateTime now = DateTime.Now;
                    TimeSpan span = sysDate.Subtract(now);
                    DateTime.Now.Add(span);
                    Logger(Log.실시간, "[시간변경] : " + DateTime.Now.ToString());
                }
                *//*
            }

            Logger(Log.실시간, "====실시간 수신 끝====");
        }
        */


        // Open API 메세지 수신 이벤트
        private void OpenApiReceiveMsg(bool bIsSystemError, string nMessageCode, string szMessage)
        {
            Logger(Log.조회, "====메세지 수신 시작====");
            Logger(Log.조회, "에러여부 : {0} | 메세지코드 : {1} | 메세지명 : {2}", bIsSystemError, nMessageCode, szMessage);
            Logger(Log.조회, "====메세지 수신 끝====");
        }



        // 현재가조회 버튼 클릭하여 조회
        private void btnRetrieve_Click(object sender, EventArgs e)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("종목코드", txtStockCd.Text.Trim());

            //OpenApiRequest("주식기본정보", "OPT10001", 0, Constants.RET_GI, param);

            lblCurPrice.Text = "-";
        }


        // 프로그램 종료
        public void ReqAppStop()
        {
            Application.Exit();
        }


        // 스레드종료 버튼 클릭하여 종료
        private void ReqThreadStop()
        {
            _sttg._timer.Stop();
        }


        // 스레드시작
        public void ReqThreadStart()
        {
            // strategy thread start
            Thread strategyTh = new Thread(_sttg.StrategyStarter);
            strategyTh.Start();

            MessageBox.Show("스레드시작 성공!");
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


        private void button2_Click_1(object sender, EventArgs e)
        {
            string yyyymmdd = txtReqDt.Text.Trim();

            string reqType;
            reqType = KOACode.reqGb[cmbReq.SelectedIndex].code;

            switch (reqType)
            {
                case "00": // 직전영업일상한가풀조회
                    //_sttg.RetPoolItem1(yyyymmdd);
                    break;

                case "01": // 쌍매수풀조회
                    //_sttg.RetPoolItem2(yyyymmdd);
                    break;

                case "10": // 근접시초가조회
                    _sttg.RetBeginningPrice(yyyymmdd);
                    break;

                case "11": // 매수비율계산
                    _sttg.UpdateBuyingAmt(yyyymmdd);
                    break;

                case "12": // 자동추매풀조회
                    //_sttg.RetPoolItem4();
                    _sttg.RetPoolItem5(yyyymmdd);
                    break;

                case "13": // 종목마스터최신화
                    _sttg.RetRefreshItemMst();
                    break;

                case "20": // 풀추적
                    //_sttg.RetPoolTracking(yyyymmdd);
                    break;

                case "90": // 타이머시작
                    ReqThreadStart();
                    break;

                case "91": // 가격지표
                    _sttg.RetAverageDisparity(yyyymmdd);
                    break;

                case "92": // 종목별투자자
                    _sttg.RetInvestor(yyyymmdd);  
                    break;

                case "93": // 종합주가지수 조회
                    _sttg.RetMarketIndex(yyyymmdd);
                    break;

                case "99": // 테스트                                        
                    //_sttg.RetInvestorLong("20161111", "20161212");  
                    //_sttg.RetAverageDisparityLong(yyyymmdd);
                    //_sttg.RetMarketIndexAbroad(yyyymmdd);
                    _sttg.RetMarketIndexInvestor("20150101", "20151231");
                    break;

                default:
                    break;
            }
        }


        //장 개시후 프로그램 시작했을 때, 프로그램 가동 여부 변경
        private void button1_Click(object sender, EventArgs e)
        {
            _bizDayYn = true;

            string today = DateTime.Now.ToString("yyyyMMdd");

            _sttg.UpdatePastBoughtItem(today);
            _sttg.UpdateTranDayCal(today);
        }
    }
}