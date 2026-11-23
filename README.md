# Combo Timer (Wayne)

在 Dalamud 搜尋 **Combo Timer (Wayne)**。停用 Simple Tweaks 的 **Combo Timer** 小功能可避免顯示兩組倒數；其他小功能可以繼續使用。輸入 `/waynecombo` 開啟外觀設定。

- Viper（PvE）持續顯示玩家身上的 Honed Steel 或 Honed Reavers 剩餘秒數。打第二、第三段、按鍵變化或亮框暫時消失時，都會繼續倒數。取得新的對應強化時，數字會同步更新為遊戲內實際的剩餘時間。自身增益剛刷新時的負值時間會以絕對值顯示，避免等待伺服器確認期間短暫消失；插件沒有 60 秒顯示上限。強化到期或消失時結束倒數。勾選 Always visible 可在沒有倒數時顯示 0。
- 其他職業與 PvP 使用 Simple Tweaks 的通用連段倒數與等級判定，含原有的龍騎、赤魔例外。龍騎、機工、武士與赤魔的升級／強化技能會對應回遊戲連段表中的基礎技能，讓強化後的連段也能顯示倒數。等級判定會排除舊技能及 NPC 專用資料。
- 首次載入時讀取本機 Simple Tweaks Combo Timer 的外觀設定。往後的調整儲存在此插件自己的設定檔。
- 遊戲內驗證可分別使用兩個起手技，觀察對應強化的倒數。接第二、第三段或切換其他技能時，數字應持續與增益剩餘時間一致，並在強化更新或到期時同步改變。

## Source and license

The native HUD layout and non-Viper timer logic are adapted from [Caraxi/SimpleTweaksPlugin, ComboTimer](https://github.com/Caraxi/SimpleTweaksPlugin/blob/d6a27f1acf7eb0da4a89459fe23960a9ded769fb/Tweaks/UiAdjustment/ComboTimer.cs). Credit to Caraxi and the Simple Tweaks contributors. This standalone adaptation adds continuous Viper enhancement tracking and is distributed under the [GNU Affero General Public License v3.0](LICENSE). Source for this release is available in this repository.

## PvE combo data audit

Version 1.0.0.4 was checked against the current game's Action and Trait data for all combat jobs, including basic-class predecessors, area combos and level sync. Data checks cover PLD, WAR, DRK, GNB, DRG, NIN, MCH, SAM, RDM, DNC, RPR, PCT and BST. VPR uses the Honed enhancement tracking described above. MNK, BRD, BLM, SMN, WHM, SCH, AST, SGE and BLU use the generic timer when the game supplies a native combo. In-game HUD verification is still required.
