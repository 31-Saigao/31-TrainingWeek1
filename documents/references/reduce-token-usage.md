# 降低 Token 用量與 API 成本

## 1. 模型分流:簡單任務用便宜模型

貴的旗艦模型只留給真正難的任務(複雜重構、架構決策),讀檔、改小地方、分類、摘要這類雜事交給便宜/低推理的模型即可。原文實測光這招每月省下約 $180。

**Claude Code**

1. 對話中輸入 `/model` 切換當前 session 的模型(Opus / Sonnet / Haiku)
2. 啟動時直接指定:`claude --model haiku`
3. 日常讀檔、跑雜活用 Haiku;要寫程式/規劃再切回 Sonnet;只有真的卡住才動用 Opus(約佔 5–10% 工作量)

**Codex**

1. 對話中輸入 `/model`,同時選**模型**與**推理強度**(`model_reasoning_effort`:minimal / low / medium / high)
2. 啟動時指定:`codex -m gpt-5-codex`;單次調推理強度:`codex -c model_reasoning_effort="low"`
3. 雜活用 `low` 推理、複雜任務才拉到 `high`——**降推理強度**在 Codex 就等同於「換便宜模型」

⚠️ 不要無腦全用最便宜的模型(見文末「沒有用的省法」)——寫程式品質掉下來,回頭修的成本比省下的還多。

---

## 2. 善用 Prompt Caching(輸入成本可省 60–85%)

模型供應商對「**重複出現的前綴內容**」(系統提示、工具定義、專案記憶檔)給快取折扣,命中時這段輸入便宜約九成。這招不用下指令,是**用習慣換折扣**——重點是別把快取打掉:

1. **同一個工作階段內不要改專案記憶檔**(`CLAUDE.md` / `AGENTS.md`)——你一改,整段前綴的位元組就變了,快取當場失效,下一回合全額重算
2. **把易變的東西放在對話裡,別塞進系統提示 / 記憶檔**——例如當下的錯誤訊息貼在訊息中就好,不要寫進 `CLAUDE.md`
3. **寧可一個長 session,不要開一堆短 session**——快取有存活時間(約 5 分鐘,延長快取約 1 小時),連續工作才吃得到命中

⚠️ 「做到一半順手精修 `CLAUDE.md`」是最常見的破快取行為——要修就等這段任務告一段落、或開新 session 再修。

---

## 3. 清理 context:別讓讀過的東西一直跟著跑

工作 90 分鐘後,累積的 context 可能有六成是**早就用不到的舊檔案內容**,卻每一回合都被重送。主動清掉:

**Claude Code**

1. 不同任務之間輸入 `/clear`,把 context 整個歸零(免費操作,等於開新對話但留在同一個終端機)
2. context 太長又還要延續時,用 `/compact` 讓模型把前面壓成摘要再繼續
3. 探索/大範圍找檔案的工作**丟給 subagent 去做**,它在獨立 context 裡讀完只回摘要,雜訊不會塞回主對話(原文實測每回合輸入省約 40%)
4. 直接下指令:「**不要重讀我們剛剛討論過的檔案,除非我特別提到**」——模型會照做

**Codex**

1. 不同任務之間用 `/clear`(或 `/new` 開全新對話串)
2. context 太長用 `/compact` 壓縮
3. 探索工作交給 subagent(`.codex/agents/*.toml`,見 [agent-configuration-codex.md](agent-configuration-codex.md));唯讀探索設 `sandbox_mode = "read-only"` 更安全
4. 同樣可直接下「不要重讀已討論過的檔案」的指示

⚠️ 修 bug、加功能換到**不相關**的主題時,先 `/clear` 再開始——不然舊主題的整包 context 會繼續被計費到新任務上。

---

## 4. 精簡專案記憶檔(每回合都在載入的東西)

`CLAUDE.md` / `AGENTS.md` 每個回合都會被讀進 context。一份 1,500 行的記憶檔約多 6K token/回合,50 回合就白燒 300K token。

- **目標長度 200–400 行**(多數專案 50–100 行就夠)
- **刪掉**:過時的決策與沿革、模型本來就會的通用建議(「要寫乾淨的程式」)、冗長的檔案結構樹、註解掉的實驗性規則
- **保留**:含版本號的技術棧、還在用的慣例(最多 10 條)、「絕對不要做」清單(最多 5 條)、常用指令速查

怎麼精修、巢狀拆檔、檔案階層的細節,見 [agent-configuration.md](agent-configuration.md)(Claude Code)或 [agent-configuration-codex.md](agent-configuration-codex.md)(Codex)。

⚠️ 記憶檔越肥不只越貴,**重點也越容易被稀釋**——關鍵慣例被淹沒在廢話裡,agent 反而更常做錯。

---

## 5. Subagent 要節制

每開一個 subagent,成本約是單一 agent 的 3–6 倍(它有自己整包的 context)。所以:

1. **只在真的能靠並行受益時才開**,而且開最少的量——多數探索任務 **4 個以內**就夠,很少需要更多
2. **subagent 預設掛便宜/低推理的模型**:
   - Claude Code:在 `.claude/agents/*.md` 的 frontmatter 指定 `model`(如 Haiku)
   - Codex:在 `.codex/agents/*.toml` 指定 `model` 或 `model_reasoning_effort`;並行上限由 `agents.max_threads` 控制(預設 6)
3. 唯讀性質的 subagent(如 code-reviewer)本來就適合用便宜模型 + 唯讀沙盒,省錢又更安全

⚠️ 「多開幾個 agent 一起衝比較快」在**成本**上通常不划算——並行省的是你的時間,不是 token。沒有並行需求就別開。

---

## 6. 批次模式(重複性跨檔任務,省 50%)

同一種操作要套到一大堆檔案上(例如替 200 個檔補 docstring、統一套某種轉換)時,別在互動模式裡一個一個跑——改用 **Batch API**,非同步在 24 小時內處理完,單價打五折。

- **Claude Code / Anthropic**:Message Batches API
- **Codex / OpenAI**:Batch API

作法是先請 agent 產生一份 `batch.jsonl`(只出 prompt、先不要執行),用腳本送出批次,再請 agent 把結果套回檔案。原文在 200+ 檔的內部掃描上實測省約 60%。

⚠️ 這是**進階/選用**技巧,牽涉自寫送批腳本,且結果最長要等 24 小時——只有在「大量、重複、不急」的任務才划算,平常互動開發用不到。

---

## 7. 訂閱制 + API 混用(個人開發者)

固定月費的訂閱方案吃日常互動,變動的重活才走按量計費的 API key:

- **日常 IDE 開發**(每週 5–10 小時內)走訂閱方案,成本固定、不怕爆
- **長批次任務、CI pipeline、多 agent 實驗**走 API key,量大但按需付費

**Claude Code**:Pro/Max 訂閱 vs. API key,用登入身分切換(`claude` 登入訂閱帳號 / 設定 API key 走按量計費)。
**Codex**:ChatGPT 方案(Plus/Pro)vs. API key,`codex login` 走 ChatGPT 登入、或改用 API key。

⚠️ 判斷點是**每週用量**:約 10 小時/週以下的個人開發,訂閱通常較划算;團隊或 20+ 小時/週的重度使用,優化過的 API 才有彈性。

---

## 沒有用(甚至更貴)的省法

- **全部改用最便宜的模型**:寫程式品質掉下來,回頭修的成本比省下的多
- **把 prompt 砍到極短**:context 不足 → 產出不到位 → 反覆重來,總 token 反而更多
- **本地跑開源模型**:速度慢好幾倍、工具使用能力較弱,拖慢的工時比省的 API 費貴
- **關掉自動帶入 context、改手動貼**:手貼比自動納入更耗 token,還容易漏

---

## 導入優先順序(投報率由高到低)

1. **Prompt caching**:習慣改掉就有,單招最大——別在任務中途改記憶檔
2. **模型分流**:讀檔/雜活用便宜模型或低推理,寫程式才用旗艦
3. **精簡記憶檔**:砍到 200–400 行
4. **清理 context**:任務間 `/clear`、探索丟 subagent
5. **Subagent 節制**:最多開 4 個、預設掛便宜模型
6. **批次模式**:重複性跨檔任務才用
7. **訂閱 + API 混用**:個人開發者按每週用量選

---

## 監控用量(避免默默失控)

隨手看一眼當前花費,異常暴增時才抓得到是哪次操作出事:

- **Claude Code**:對話中 `/cost` 看本 session 的 token 與花費;整體趨勢看 Anthropic Console
- **Codex**:對話中 `/status`、`/usage` 看用量;整體趨勢看 OpenAI 用量儀表板

建議:設每月花費上限、當週花費比基準高出 30% 以上時就回頭查「這週改了什麼」。
