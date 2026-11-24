# Combo Timer (Wayne)

在 Dalamud 搜尋 **Combo Timer (Wayne)**，用 `/waynecombo` 開啟設定。整合版沿用原本的插件識別及更新網址。

七個功能各有獨立勾選開關及設定頁。

- 連段倒數。Viper PvE 持續追蹤 Honed Steel／Honed Reavers；其他職業使用遊戲通用連段時間，包含升級技能對應與等級同步判定。
- 敵人清單血量。
- 敵人清單中由自己施加的狀態及倒數。
- 目標、暫選目標、焦點及滑鼠目標的距離顯示，可建立多個項目。
- 放大自己施加在目前目標上的狀態圖示。
- DTR FPS 顯示與歷史統計。
- Ping 浮窗、歷史圖及 DTR 顯示。

設定視窗右上角有摺疊／展開按鈕。摺疊後保留標題列，各項功能繼續運作。

## 1.1.0.1

- Settings navigation uses the original English feature/plugin names.
- EnemyListDebuffs no longer writes its width-measurement sample into a displayed timer. Empty groups stay hidden, and countdown font spacing matches the original plugin.
- PingPlugin falls back to the current data center's lobby address when the Windows TCP table cannot identify the game server, as in the original PingPlugin. Settings identify fallback measurements because a proxy can make their latency differ from the actual game connection.
- A failed ping method no longer discards a successful result from the other method. An average with no samples displays `-- ms`.

## Existing settings

第一次從舊版 Combo Timer 升級時，自動讀取 EnmityHp、EnemyListDebuffs、Distance、BigPlayerDebuffs、FPSPlugin、PingPlugin 的設定，保留原有 Combo Timer 外觀。位置、大小、顏色、對齊、圖示尺寸及顯示條件會轉入整合版。Ping 浮窗實際拖曳後的位置會一併從 Dalamud 的介面位置檔讀取。

匯入完成後，設定保存在整合版的設定檔。左下角的「匯入原設定」可重新讀取。

當 EnmityHp、EnemyListDebuffs、Distance、BigPlayerDebuffs、FPSPlugin 或 PingPlugin 仍載入時，整合版對應模組會暫停並在設定頁提示。停用原插件後，該模組會自動接手。這可避免重複顯示與同時修改同一處原生介面。

首次匯入時，找到原設定檔的功能會依原本開關啟用。其餘功能可在整合設定中勾選。

## Verification

The regression runner covers the existing combo rules, Viper refresh durations, legacy JSON metadata, Distance backing-field/collection formats, position and color round trips, saved Ping window positions, ping aggregation, independent backend failures, cancellation, server-port filtering and data-center fallback. The plugin builds against Dalamud API 15.

The current user's six source settings were checked locally against the imported models. In-game native HUD placement, resize behavior and network measurements require live game verification.

## Source and license

The original combo timer HUD and non-Viper logic are adapted from [Caraxi/SimpleTweaksPlugin](https://github.com/Caraxi/SimpleTweaksPlugin/blob/d6a27f1acf7eb0da4a89459fe23960a9ded769fb/Tweaks/UiAdjustment/ComboTimer.cs), with credit to Caraxi and the Simple Tweaks contributors.

Enemy status layout and Distance-compatible anchoring follow their MIT-licensed upstreams. Server detection and ping aggregation are adapted from PingPlugin. Required notices are included in [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt).

The combined plugin is distributed under the [GNU Affero General Public License v3.0](LICENSE). Corresponding source is available in this repository.
