# OrderHub

練習說明入口請看 **[documents/README.md](documents/README.md)**

## 分支規則

各位請開自己的 branch 進行練習，**不要直接 commit 到 `main`**。

分支名稱格式為你的員工編號：`IM00XX`。

## 開 branch 流程

1. Clone 專案並進入目錄：

   ```powershell
   git clone https://github.com/sox6769/traning.git
   cd traning
   ```

2. 從最新的 `main` 開出自己的 branch（把 `IM00XX` 換成你的員工編號）：

   ```powershell
   git switch main
   git pull
   git switch -c IM00XX
   ```

3. 在自己的 branch 上進行練習並 commit：

   ```powershell
   git add .
   git commit -m "你的 commit 訊息"
   ```

4. 推上遠端（第一次 push 需加 `-u` 建立追蹤）：

   ```powershell
   git push -u origin IM00XX
   ```
