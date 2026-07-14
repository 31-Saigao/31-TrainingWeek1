# 進階指南 — 把 Codex CLI「調教」成你的專案隊友

> 這份指南教你**怎麼設定 agent 的環境**——
> 用設定檔讓它自動遵守專案慣例、擋掉危險操作、把重複的流程變成一鍵指令。
> 內容與 [agent-configuration.md](agent-configuration.md)（Claude Code 版）一一對應，用哪個工具就看哪份。
> 每一節都有可以直接複製到本專案的範例，建議邊讀邊做。

**你會建立的檔案總覽**

| 檔案                              | 用途                                             | 要不要進 git          |
| --------------------------------- | ------------------------------------------------ | --------------------- |
| `AGENTS.md`                       | 專案記憶：agent 每次啟動自動讀的專案說明與慣例   | ✅ 進 git（全隊共用） |
| `.codex/config.toml`              | 專案層設定：hooks 等（approval / sandbox 在專案層無效） | ✅ 進 git             |
| `.codex/rules/*.rules`            | 指令規則：哪些指令直接放行、詢問、直接擋掉       | ✅ 進 git             |
| `.codex/agents/*.toml`            | Subagents：專職的子代理（如 code reviewer）      | ✅ 進 git             |
| `.agents/skills/<名稱>/SKILL.md`  | Skills：把流程做成可重複觸發的指令               | ✅ 進 git             |
| `~/.codex/config.toml`            | 個人全域設定：approval / sandbox 等安全設定放這裡 | ❌ 在家目錄，不進 git |

**與 Claude Code 的對照**（兩邊都玩過的人看這張表就懂）

| 概念           | Claude Code                      | Codex CLI                             |
| -------------- | -------------------------------- | ------------------------------------- |
| 專案記憶       | `CLAUDE.md`                      | `AGENTS.md`                           |
| 權限規則       | `.claude/settings.json` 的 allow/ask/deny | `approval_policy` + `sandbox_mode` + `.codex/rules/`（execpolicy） |
| Hooks          | settings.json 的 `hooks`         | `.codex/config.toml` 或 `.codex/hooks.json` 的 `hooks` |
| Subagents      | `.claude/agents/*.md`（Markdown） | `.codex/agents/*.toml`（TOML）        |
| 斜線指令／流程 | `.claude/skills/*/SKILL.md`      | `.agents/skills/*/SKILL.md`（舊的 `~/.codex/prompts/` custom prompts 官方文件已不再收錄） |

> **注意**：專案層的 `.codex/` 設定要在你**信任（trust）這個專案**之後才會載入——第一次在 repo 裡啟動 `codex` 時會問你是否信任此資料夾。

---

## 1. `AGENTS.md` — 讓 agent 不用每次重新認識專案

Codex 的專案記憶檔叫 **`AGENTS.md`**（這是跨工具的開放格式，Codex 原生支援）。放在**專案根目錄**，agent 每個 session 開始時自動載入。把「每次都要重講一遍」的東西寫進來：架構慣例、常用指令、地雷區。

在 `training-repo` 建立 `AGENTS.md`：

```
codex       # 啟動（首次會要求登入，並詢問是否信任此專案）
/init       # 在對話框輸入，自動產生 AGENTS.md
```

**進階技巧**

- 使用者層級的個人偏好放 `~/.codex/AGENTS.md`，對你所有專案生效（例如「我的 SQL Server 是具名實例 .\SQLEXPRESS」這種個人環境差異）
- 子目錄也可以放自己的 `AGENTS.md`，處理該目錄時會一併載入——大型 repo 可用來寫各模組的局部慣例

**驗證方式**：

- [ ] 建好後開新 session，直接問 agent「這個專案的分層慣例是什麼？」——它應該不用讀任何檔案就答得出來。

---

## 2. Approval + Sandbox + Rules — 先劃紅線，再開綠燈

Claude Code 用一份 allow/ask/deny 清單管權限；Codex 拆成**三層**，各管一件事：

1. **`sandbox_mode`**：OS 層沙盒，管 agent「技術上做得到什麼」（能寫哪裡、能不能連網）
2. **`approval_policy`**：管「什麼時候要問你」
3. **execpolicy rules**：對**個別指令**做細部規則——危險的直接擋掉（forbidden）、日常的直接放行（allow）、重大的強制詢問（prompt）

### 2a. 基本設定（`~/.codex/config.toml`）

在**使用者層** `~/.codex/config.toml` 設定（就是家目錄那份，不是專案層）：

```toml
# 沙盒：只能寫 workspace 內的檔案，預設不能連網
sandbox_mode = "workspace-write"

# 要跳出沙盒（連網、寫 workspace 外）或碰到 rules 標記的指令時詢問
approval_policy = "on-request"
```

> **為什麼不是放專案層？** `sandbox_mode` 和 `approval_policy` 屬於安全設定，**寫在專案層 `training-repo/.codex/config.toml` 會被 Codex 忽略**（官方 config reference 明列專案層不允許覆寫這些 key）。專案層 config 適合放 hooks 這類可以進 git 共用的設定。

- `sandbox_mode` 可選 `read-only`（只能讀）、`workspace-write`（預設，可寫工作區）、`danger-full-access`（拆掉沙盒，**不要用**）
- `approval_policy` 可選 `untrusted`（除了安全的讀取操作外都問）、`on-request`（要升權時問）、`never`（都不問，**練習中不要用**）
- 沙盒內預設**擋網路**；真的需要時才在 `[sandbox_workspace_write]` 加 `network_access = true`

> **Windows 注意**：Codex 的沙盒在 macOS 是 Seatbelt、Linux 是 `bwrap` + `seccomp`（0.115 起，WSL1 已不支援），原生 Windows 的沙盒支援較新、模式不同（unelevated / elevated），官方另推薦 WSL2。本練習在 Windows 上請以 `approval_policy` + rules 為主要防線，並實際驗證沙盒行為。

### 2b. 指令規則（`.codex/rules/orderhub.rules`）

Rules 用 Starlark 語法（長得像 Python）寫**前綴比對**規則。在 `training-repo/.codex/rules/orderhub.rules` 建立：

```python
# ---- 危險操作：直接擋掉 ----
prefix_rule(
    pattern = ["git", "push", "--force"],
    decision = "forbidden",
    justification = "禁止強推，需要時請人工操作",
)
prefix_rule(
    pattern = ["git", "reset", "--hard"],
    decision = "forbidden",
    justification = "會丟失未 commit 的變更",
)

# ---- 重大操作：強制詢問 ----
prefix_rule(
    pattern = ["dotnet", "ef", "database", "drop"],
    decision = "prompt",
    justification = "重置資料庫必須由人確認",
)
prefix_rule(
    pattern = ["git", "push"],
    decision = "prompt",
    justification = "推上遠端前先確認",
)

# ---- 日常操作：直接放行 ----
prefix_rule(pattern = ["dotnet", "build"], decision = "allow", justification = "日常建置")
prefix_rule(pattern = ["dotnet", "test"],  decision = "allow", justification = "日常測試")
prefix_rule(pattern = ["dotnet", "run"],   decision = "allow", justification = "啟動網站")
prefix_rule(pattern = ["git", "status"],   decision = "allow", justification = "唯讀")
prefix_rule(pattern = ["git", "diff"],     decision = "allow", justification = "唯讀")
prefix_rule(pattern = ["git", "log"],      decision = "allow", justification = "唯讀")
prefix_rule(pattern = ["git", "add"],      decision = "allow", justification = "日常提交流程")
prefix_rule(pattern = ["git", "commit"],   decision = "allow", justification = "日常提交流程")
```

**規則語法重點**

- `pattern` 是**前綴比對**：`["dotnet", "build"]` 匹配所有以 `dotnet build` 開頭的指令
- 多條規則同時命中時，取**最嚴格**的決定：`forbidden` > `prompt` > `allow`——所以 `git push --force` 被 forbidden 擋下，一般 `git push` 走 prompt
- 注意：前綴比對不是完整沙盒，串接指令（如 `;`、`&&`）可能繞過，不應視為絕對安全防線——這也是為什麼還需要 sandbox 和 hooks
- 寫完可以**離線測試**規則檔（不用真的啟動 agent）：

```powershell
codex execpolicy check --pretty --rules .codex/rules/orderhub.rules -- git push --force
```

**為什麼這樣設計**

- `forbidden` git reset --hard：「順手清理一下」會毀掉你還沒 commit 的練習成果
- `prompt` database drop：練習中真的需要重置資料庫，但必須是**人**按下確認
- `allow` build/test：這些指令 agent 會反覆執行，每次都問只會讓你麻木地按 yes——**權限疲勞正是事故的來源**

**驗證方式**：

- [ ] 用 `codex execpolicy check ... -- git push --force` 確認結果是 `forbidden`
- [ ] 請 agent 執行 `dotnet test`，應直接執行不詢問（allow）
- [ ] 請 agent 重置資料庫（`dotnet ef database drop`），應先跳出確認才執行（prompt）

---

## 3. Hooks — 用程式強制執行，不靠 agent 自覺

AGENTS.md 裡的規則 agent「通常」會遵守；hooks 則是**由 Codex 本身強制執行**的檢查點，agent 想繞也繞不過。Codex 的 hooks 可以寫在 `.codex/config.toml` 的 `[hooks]` 區塊，或獨立的 `.codex/hooks.json`（JSON 格式與 Claude Code 幾乎相同）。

**常用事件**：`PreToolUse`（工具執行前，可攔截）、`PostToolUse`（工具執行後）、`UserPromptSubmit`（你送出訊息時）、`SessionStart`、`Stop`（agent 結束回合時）。

### PreToolUse & PostToolUse 範例

rules 擋的是「指令長相」，hook 可以檢查**內容**。在 `training-repo/.codex/hooks.json` 建立：

```json
{
  "hooks": {
    "PreToolUse": [
      {
        "matcher": "Bash",
        "hooks": [
          {
            "type": "command",
            "command": "powershell -NoProfile -Command \"$in = [Console]::In.ReadToEnd(); if ($in -match 'DROP TABLE|TRUNCATE') { [Console]::Error.WriteLine('禁止破壞性 SQL，請改用 EF migration 或重置腳本'); exit 2 } exit 0\"",
            "statusMessage": "檢查 SQL 指令"
          }
        ]
      }
    ],
    "PostToolUse": [
      {
        "matcher": "apply_patch",
        "hooks": [
          {
            "type": "command",
            "command": "dotnet build --nologo -v q"
          }
        ]
      }
    ]
  }
}
```

- `PreToolUse` 攔截任何含 `DROP TABLE` / `TRUNCATE` 的指令（**exit code 2 = 擋下這次工具呼叫**，stderr 訊息會回饋給 agent；也可以輸出 JSON 回傳 `permissionDecision: "deny"`）
- `PostToolUse` 每次 agent 用 `apply_patch` 改完檔案就自動 build，錯誤立即回饋給 agent（它會自己修）。注意 Codex 的檔案編輯工具叫 `apply_patch`（不是 Claude 的 Edit/Write）
- 其他非 0、非 2 的 exit code 視為 hook 本身故障，Codex 會照常繼續，不會擋下操作

> 複雜邏輯建議寫成獨立腳本放 `.codex/hooks/`，hook 的 `command` 再去呼叫它。

**驗證方式**：

- [ ] 設定好後，故意請 agent「執行 sqlcmd 把 Orders 資料表 TRUNCATE 掉」——它應該被 hook 擋下，並回報被擋的原因。

---

## 4. Subagents — 建立專職的子代理

Subagent 是有**獨立 context、獨立沙盒權限**的子代理。兩個經典用途：

1. **唯讀 reviewer**：`sandbox_mode = "read-only"`，物理上不可能改壞你的程式碼
2. **隔離大量輸出**：跑測試、查大量檔案的雜訊留在子代理，不塞爆主對話

Codex 的 subagent 是**一個 agent 一個 TOML 檔**。在 `training-repo/.codex/agents/code-reviewer.toml` 建立：

```toml
name = "code-reviewer"
description = "審查程式碼變更是否符合 OrderHub 分層慣例。完成 bug 修復或新功能後主動使用。"
sandbox_mode = "read-only"
developer_instructions = """
你是 OrderHub 專案的資深 reviewer。審查目前的變更（git diff），依序檢查：

1. 分層：商業邏輯是否在 Core 的 service？Controller 是否保持薄？
   有沒有在 service/controller 直接使用 DbContext？
2. View 是否綁 ViewModel 而非 domain model？
3. 驗證是否用 DataAnnotations + ModelState（使用者輸入不可造成 500）？
4. 金額是否使用 decimal？
5. 有沒有對應的測試？測試是否真的驗證了行為（不是恆真斷言）？

輸出：依嚴重度排序的問題清單，每項附檔案:行號與具體修改建議。沒問題就明說。
"""
```

再建一個 `test-runner.toml`（把測試雜訊隔離在子代理）：

```toml
name = "test-runner"
description = "執行 dotnet test 並回報摘要。需要跑測試驗證時使用。"
developer_instructions = """
執行 `dotnet test`。全綠時只回報「N 個測試全部通過」。
有失敗時：列出失敗的測試名稱、斷言訊息、以及你判斷的可能原因，不要貼完整輸出。
"""
```

**欄位重點**：`name`、`description`、`developer_instructions` 必填（description 決定 agent 何時會主動委派給它）；`model_reasoning_effort`、`sandbox_mode` 選填，不填就繼承主 session。並行數量由 `agents.max_threads` 控制（預設 6）。

**驗證方式**：修完一個 bug 後說「用 code-reviewer 審查我的變更」，或直接觀察 agent 會不會在適當時機自己委派。

---

## 5. Skills — 把重複流程做成一鍵指令

練習 2 每個 bug 都要走同一套流程，第二次開始你就會想把它做成指令。Codex 的 skill 是「一個資料夾 + `SKILL.md`」，專案層放在 **`.agents/skills/`**（注意：是 `.agents/`，不是 `.codex/`；個人全域的放 `~/.agents/skills/`）。

> 你可能在網路教學看到 `~/.codex/prompts/*.md` 的 custom prompts 寫法——官方文件已**不再收錄**這種做法，新流程請一律用 skills。

在 `training-repo/.agents/skills/fix-bug/SKILL.md` 建立：

```markdown
---
name: fix-bug
description: 依標準流程修復一個 bug：重現、定位、修復、回歸測試、commit。使用者明確要求修 bug 時才使用。
---

依照以下流程修復使用者描述的 bug（症狀在使用者的訊息裡）：

1. 先根據症狀推測涉及的頁面與流程，向使用者確認你對症狀的理解
2. 從 Controller 往下追到 Service、Repository，定位根因；
   說明根因後**等使用者確認**再動手修
3. 用最小變更修復，不要順手重構無關的程式碼
4. 補一個回歸測試（先確認它在修復前會失敗的邏輯），跑 `dotnet test` 確認全綠
5. 提示使用者回頁面實測，確認後以「症狀 → 根因 → 修法」格式撰寫 commit message 並 commit
```

之後輸入 `$fix-bug 訂單列表第一頁看不到新訂單`（或透過 `/skills` 選單挑選）就會啟動整套流程。

再做一個驗收用的 `verify-exercise`（`.agents/skills/verify-exercise/SKILL.md`）：

```markdown
---
name: verify-exercise
description: 檢查練習交付物是否齊備：測試全綠、commit 紀律、PROCESS.md 有填。只在使用者明確要求驗收時使用。
---

檢查目前 repo 是否符合活動驗收標準，逐項回報通過/未通過：

1. `dotnet test` 全綠
2. `git log --oneline` 中每個 bug 修復與新功能是否各自獨立 commit，
   message 是否說明症狀與根因
3. PROCESS.md 是否有實質內容（不是空範本）
4. /Products/LowStock 相關程式是否遵循分層慣例
```

**格式重點**

- frontmatter 的 `name`、`description` 必填；`name` 建議用 kebab-case（全小寫、連字號）並與資料夾名稱保持一致
- Codex 也會**自動觸發** skill：任務內容符合 `description` 描述時它會自己選用——所以 description 要寫清楚「什麼時候該用、什麼時候不該用」，不想被自動觸發就在 description 明說「使用者明確要求時才使用」
- Skill 採「漸進載入」：平常只載入 name + description，被選用時才讀完整內容——所以主要說明寫在內文，不要塞在 description
- 資料夾裡可以放輔助腳本或參考文件，SKILL.md 內文用相對路徑引用

---

## 附：官方文件

- 設定總覽與 `config.toml` 參考：https://learn.chatgpt.com/docs/config-file/config-reference
- Approvals 與沙盒：https://learn.chatgpt.com/docs/agent-approvals-security
- Rules（execpolicy）：https://learn.chatgpt.com/docs/agent-configuration/rules
- Hooks：https://learn.chatgpt.com/docs/hooks
- Subagents：https://learn.chatgpt.com/docs/agent-configuration/subagents
- Skills：https://learn.chatgpt.com/docs/build-skills
