# GameSetupEditor バックアップ情報

**作成日**: 2026-05-15  
**目的**: エンコーディング破損に備えたバックアップ

---

## バックアップファイル一覧

### 1. GameSetupEditor.cs（本体）
- **パス**: `Assets/Editor/GameSetupEditor.cs`
- **状態**: ✅ 正常動作（UTF-8 BOM無し）
- **説明**: 現在使用中のファイル

### 2. GameSetupEditor.cs.backup（プライマリバックアップ）
- **パス**: `Assets/Editor/GameSetupEditor.cs.backup`
- **状態**: 🔒 無効化（Unityは無視）
- **説明**: 同じフォルダ内のバックアップ

### 3. GameSetupEditor.cs.txt（セカンダリバックアップ）
- **パス**: `.github/doc/GameSetupEditor.cs.txt`
- **状態**: 🔒 無効化（Unityは無視・別フォルダ）
- **説明**: ドキュメントフォルダのテキスト形式バックアップ

### 4. GameSetupEditor.cs.broken（破損版）
- **パス**: `Assets/Editor/GameSetupEditor.cs.broken`
- **状態**: ❌ 文字化け（UTF-8 BOM付き・文字化け）
- **説明**: 参考用・削除可能

---

## 復元方法

### ケース1: GameSetupEditor.csが破損した場合

#### 方法A: プライマリバックアップから復元
```powershell
Copy-Item -Path "Assets/Editor/GameSetupEditor.cs.backup" -Destination "Assets/Editor/GameSetupEditor.cs" -Force
```

#### 方法B: セカンダリバックアップから復元
```powershell
Copy-Item -Path ".github/doc/GameSetupEditor.cs.txt" -Destination "Assets/Editor/GameSetupEditor.cs" -Force
```

### ケース2: Unity Editorで復元
1. Unityを閉じる
2. エクスプローラーで `Assets/Editor/` を開く
3. `GameSetupEditor.cs.backup` を `GameSetupEditor.cs` にリネーム（上書き確認）
4. Unityを再起動

---

## バックアップの更新

GameSetupEditor.csに変更を加えた後は、バックアップも更新してください：

```powershell
# プライマリバックアップ更新
Copy-Item -Path "Assets/Editor/GameSetupEditor.cs" -Destination "Assets/Editor/GameSetupEditor.cs.backup" -Force

# セカンダリバックアップ更新
Copy-Item -Path "Assets/Editor/GameSetupEditor.cs" -Destination ".github/doc/GameSetupEditor.cs.txt" -Force
```

---

## エンコーディング確認方法

### PowerShellでBOMチェック
```powershell
$bytes = [System.IO.File]::ReadAllBytes("Assets/Editor/GameSetupEditor.cs")
if ($bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
    Write-Host "UTF-8 BOM付き（要注意）"
} else {
    Write-Host "UTF-8 BOM無し（正常）"
}
```

### 正しい保存形式
- ✅ **UTF-8 BOM無し**（推奨）
- ❌ UTF-8 BOM付き（文字化けリスク）
- ❌ Shift-JIS（Unityで非推奨）

---

## 予防策

1. **IDEの設定確認**
   - Visual Studio: Tools > Options > Text Editor > Advanced > "Save as UTF-8 without signature"
   - Rider: Settings > Editor > File Encodings > "UTF-8" + "Without BOM"

2. **定期的なバックアップ**
   - 大きな変更の前後にバックアップを更新

3. **バージョン管理**
   - Gitを使用している場合は、コミット前に必ず確認

---

## 問題が発生した場合

1. **文字化けが発生**
   → バックアップから即座に復元

2. **コンパイルエラー大量発生**
   → エンコーディングが破損している可能性
   → バックアップから復元

3. **バックアップも破損**
   → `.github/doc/game-setup-editor-design.md` を参照して再作成
   → または `.github/doc/GameSetupEditor.cs.txt` から復元

---

## ファイルサイズ一覧（2026-05-15時点）

| ファイル | サイズ | 状態 |
|---------|--------|------|
| GameSetupEditor.cs | 24,496 bytes | ✅ 正常 |
| GameSetupEditor.cs.backup | 24,496 bytes | 🔒 バックアップ |
| GameSetupEditor.cs.txt | 24,496 bytes | 🔒 バックアップ |
| GameSetupEditor.cs.broken | 87,612 bytes | ❌ 破損 |

**注意**: 正常ファイルと破損ファイルではサイズが大きく異なります（24KB vs 87KB）

---

## 参考リンク

- **設計書**: `.github/doc/game-setup-editor-design.md`
- **再構築記録**: `.github/doc/game-setup-editor-rebuild.md`
- **コーディング規約**: `.github/copilot-instructions.md`

---

**最終更新**: 2026-05-15 18:41

