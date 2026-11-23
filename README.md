# Combo Timer (Wayne)

在 Dalamud 搜尋 **Combo Timer (Wayne)**。停用 Simple Tweaks 的 **Combo Timer** 小功能可避免顯示兩組倒數；其他小功能可以繼續使用。輸入 `/waynecombo` 開啟外觀設定。

- Viper（PvE）使用玩家身上的 Honed Steel 或 Honed Reavers 剩餘秒數。對應的單體快捷鍵必須仍是 Steel Fangs 或 Reaving Fangs，且遊戲判定它正在亮框，才顯示數字。按鍵變為後續招式或亮框消失就隱藏。起手再度亮起時會接著顯示增益當下的剩餘時間。
- 其他職業與 PvP 使用 Simple Tweaks 的通用連段倒數與等級判定，含原有的龍騎、赤魔例外。
- 首次載入時讀取本機 Simple Tweaks Combo Timer 的外觀設定。往後的調整儲存在此插件自己的設定檔。
- 遊戲內驗證可分別使用兩個起手技，接完三段後觀察下一個亮框起手。倒數應與對應增益一致。若特定等級無法重現，請記錄角色同步後的等級、兩個按鍵上實際顯示的技能，以及增益名稱。

## Source and license

The native HUD layout and non-Viper timer logic are adapted from [Caraxi/SimpleTweaksPlugin, ComboTimer](https://github.com/Caraxi/SimpleTweaksPlugin/blob/d6a27f1acf7eb0da4a89459fe23960a9ded769fb/Tweaks/UiAdjustment/ComboTimer.cs). Credit to Caraxi and the Simple Tweaks contributors. This standalone adaptation adds Viper starter-highlight tracking and is distributed under the [GNU Affero General Public License v3.0](LICENSE). Source for this release is available in this repository.
