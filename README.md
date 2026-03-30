# Victoria 3 Data Exporter

## 概要
Victoria 3 Data ExporterはVictoria3のゲームデータをファイルを解析し、出力するツールである。
出力する形式として、PukiWikiの表形式を使用する。

---

## 構成

```
PdxScriptAnalysis          ← ゲームデータのスクリプト(Paradox Scriptと呼称する)の字句解析・構文解析
Victoria3.GameData         ← ゲームデータモデルの定義(Countryなど)
Victoria3.Loading          ← ASTからGameDataへの変換
Victoria3.Analysis         ← オブジェクト間の関係解析
Victoria3.Localization     ← 日本語ローカライズ・ノード翻訳
Victoria3.Export           ← 出力
Victoria3.App              ← CLIエントリーポイント
```

---

## PdxScriptAnalysis
Paradox Script形式のテキストをASTに変換する層。他の層はすべてこのASTを起点とする。

### 主要クラス

**ScriptTree** - 解析結果を表すクラス。ファクトリメソッドでのみ生成される。

```csharp
public sealed class ScriptTree
{
    public SourceText Source { get; }
    public RootNode Root { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public bool HasErrors { get; }

    public static ScriptTree ParseFile(string path);
    public static ScriptTree ParseText(string text);
    public static ScriptTree ParseSource(SourceText source);
}
```

**SyntaxKind** - トークン種別の列挙型。

```csharp
public enum SyntaxKind
{
    LeftBrace, RightBrace,
    Equals, LessThan, GreaterThan,
    LessThanEquals, GreaterThanEquals, NotEquals, QuestionEquals,
    StringLiteral, Atom,
    Unknown, EndOfFile,
}
```

**SyntaxNode の継承ツリー**

```
SyntaxNode(abstract record)
├─ RootNode              ファイル全体。Children にトップレベルノードを持つ。
├─ BlockNode             { ... } で囲まれた複数ノード。
├─ ScalarNode            トークン1つ(Atom または StringLiteral)。
└─ PropertyNode(abstract)  Key Operator Value の形式。
   ├─ ScalarPropertyNode     Value がスカラー値。例: tag = JPN
   ├─ BlockPropertyNode      Value がブロック。例: cultures = { ... }
   └─ TypedBlockPropertyNode Value が修飾子付きブロック。例: color = hsv { 10 20 30 }
```

**SyntaxVisitor\<TResult\>** - 戻り値ありの訪問。Visit メソッドをオーバーライドして使う。

**SyntaxWalker** - 戻り値なしの巡回。DefaultVisit が子ノードを自動巡回する。

**TextSpan** - ソーステキスト上の位置範囲(Start, Length, End)。

**SourceText** - ソーステキストのラッパー。行・列変換キャッシュ・部分文字列取得を提供する。

**Diagnostic** - 解析中のエラー・警告・情報を表す。Severity, Message, Span を持つ。

---

## Victoria3.Loading

ゲームディレクトリのパスを受け取り、GameDataのオブジェクトを返す層。

### 主要クラス

**CountryLoaderなどの各ゲームデータのローダー** - コンストラクタで渡した`ScriptTree`の列挙を`Load`で解析し、結果を`LoadOutput<T>`で返す

```csharp
public sealed class CountryLoader(IEnumerable<ScriptTree> trees)
{
    public LoadOutput<Country> Load();
}
```

**LoadOutput** - ロード結果を表す。解析されたゲームデータのリストである`Values`と診断結果のリストである`Diagnostics`を持つ。

**Victoria3Paths** - 各ゲームデータファイルの基底ゲームファイルからの相対パスを定義するクラス。

---

## Victoria3.Analysis

WIP

---

## Victoria3.Localization

単純なキーの変換と、ASTノードの変換の2つの機能を持つ。
ASTノードの変換は後で作成予定。

### 主要クラス

**ILocalizer** - キーのローカライズを提供するインターフェース。失敗時にキーをそのまま返す`Localize`と、成功か失敗かを真偽値で返す`TryLocalize`の2つのメソッドを定義する。

```csharp
public interface ILocalizer
{
    public string Localize(string key);
    public bool TryLocalize(string key, [NotNullWhen(true)] out string value);
}
```

**FileLocalizer** - ローカライズファイルを読み込み、キーを日本語文字列に変換する。静的ファクトリに翻訳辞書・翻訳ファイルのテキスト・パス・パスの列挙・ディレクトリのいずれかを渡して初期化する。翻訳辞書の作成には`LocalizationParser`を使用し、ファイルIOと変換の実行のみ行う。

```csharp
public class FileLocalizer : ILocalizer
{
    public static FileLocalizer FromLocalizations(IReadOnlyDictionary<string, string> localizations);
    public static FileLocalizer FromText(string text);
    public static FileLocalizer FromPath(string path);
    public static FileLocalizer FromPaths(IEnumerable<string> paths);
    public static FileLocalizer FromDirectory(string directoryPath);

    public string Localize(string key);
    public bool TryLocalize(string key, [NotNullWhen(true)] out string value);
}
```

**LocalizationParser** - 翻訳ファイルのテキストから翻訳辞書を作成する。

```csharp
internal class LocalizationParser
{
    internal static IReadOnlyDictionary<string, string> ParseText(string text)
}
```

**LocalizationPaths** - 翻訳ファイルのディレクトリパスを提供するクラス。

---

## Victoria3.Export

WIP

---

## Victoria3.App

CLIエントリーポイントとなる層。
現在作成中のため以下の内容は確定されたものではない。

### 使用ライブラリ

| ライブラリ | 用途 |
|---|---|
| System.CommandLine | サブコマンド・オプション・ヘルプ自動生成 |
| Tomlyn | TOML設定ファイルの読み書き |
| Spectre.Console | プログレスバー・対話形式セットアップ等 |

### コマンド構造

```
vc3tool init                          # 対話形式で設定ファイルを生成
vc3tool config show                   # 現在の設定を確認
vc3tool config set <key> <value>      # 設定値を変更
vc3tool export countries              # 国家データを出力
vc3tool export countries --output <dir>   # 出力先をその場だけ上書き
```

### 設定ファイル（vc3tool.toml）

```toml
[game]
directory = "C:/Program Files/Steam/steamapps/common/Victoria 3"

[output]
directory = "./output"

[localization]
locale = "japanese"
```

設定ファイルの探索順：
1. カレントディレクトリの `vc3tool.toml`
2. ユーザーホームの `~/.vc3tool/config.toml`
3. コマンドライン引数 `--config` で明示指定

### DIコンテナの使い方

各層のオブジェクトはDIで管理し、ILocalizerなど複数箇所から参照されるものはシングルトンとして登録することでプロセス内の暗黙的なキャッシュとする。プロセスをまたぐキャッシュ（永続化）は当面導入しない。

```csharp
services.AddSingleton<ILocalizer>(new FileLocalizer(config.LocalizationPath));
services.AddSingleton<INodeTranslator, CompositeNodeTranslator>();
services.AddTransient<CountryLoader>();
```

### エントリーポイント

```csharp
static async Task<int> Main(string[] args)
{
    var rootCommand = new RootCommand("Victoria 3 データ解析ツール");
    rootCommand.AddCommand(new InitCommand());
    rootCommand.AddCommand(new ConfigCommand());
    rootCommand.AddCommand(new ExportCommand());
    return await rootCommand.InvokeAsync(args);
}
```



