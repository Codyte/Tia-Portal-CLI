# __navi__ · src/ (C#) — símbolos públicos por arquivo
<!-- gerado por scripts/navi-cs.ps1 · regenerar após refatorar -->

## `Tia.Cli/Program.cs` (810 linhas)
Program(16)  save-project(345)  close-project(348)  info(351)  list-devices(354)  list-blocks(357)  list-tags(361)  tree(364)  list-types(368)  find(371)  snapshot(375)  xref(378)  trace(381)  list-hmi(384)  free-memory(387)  export-block(393)  explain-block(396)  export-tags(401)  import-block(404)  import-ladder(411)  import-source(419)  create-folder(424)  delete-folder(429)  delete-block(434)  move-block(438)  delete-type(443)  export-type(447)  import-type(450)  scaffold(454)  clone(463)  add-db-member(469)  import-tags(475)  create-library(482)  list-library(486)  import-master-copy(489)  add-master-copy(495)  create-instance-db(501)  delete-master-copy(506)  add-device(511)  delete-device(516)  add-tag(520)  delete-tag(526)  edit-db-member(531)  delete-db-member(537)  rename-block(542)  set-tag(547)  set-attr(554)  list-attrs(560)  plug-module(564)  list-telegrams(570)  insert-telegram(573)  set-address(580)  set-io-address(586)  set-memory-bytes(592)  connect-subnet(598)  export-cax(603)  import-cax(606)  compile(610)  diff-block(625)  audit(629)  doctor(633)  gen-profinet(641)  standardize-tags(647)  gen-fault-ob(655)  replicate-fc(663)  gen-alarm-fc(670)  replicate-instruments(678)

## `Tia.Core/AlarmFc.cs` (687 linhas)
AlarmFcConfig(17)  AlarmFc(42)  Generate(58)  LEITURA_MUITO_ALTA(648)  LEITURA_ALTA(649)  LEITURA_BAIXA(650)  LEITURA_MUITO_BAIXA(651)  SEM_4MA(652)

## `Tia.Core/AssemblyInfo.cs` (3 linhas)


## `Tia.Core/Audit.cs` (215 linhas)
Audit(20)  TagOf(38)  CarriesTag(45)  NormalizeArea(52)  Run(62)

## `Tia.Core/BlockExplain.cs` (361 linhas)
BlockExplain(22)  Explain(24)  Statements(184)  Coil(207)  SCoil(208)  RCoil(209)  Move(210)  Contact(301)  Eq(304)  O(311)  A(313)  Call(315)

## `Tia.Core/Clone.cs` (198 linhas)
Clone(24)  Run(26)  RewriteFile(160)  ParseReplaces(175)

## `Tia.Core/DbMember.cs` (310 linhas)
DbMember(24)  Add(33)  Change(79)  Remove(118)  Delta(162)  Edit(208)

## `Tia.Core/Doctor.cs` (183 linhas)
Doctor(14)  Run(23)  gen-profinet(67)  standardize-tags(84)  gen-fault-ob(93)  replicate-fc(109)  gen-alarm-fc(126)  replicate-instruments(144)

## `Tia.Core/Drives.cs` (201 linhas)
Drives(17)  ListTelegrams(84)  InsertTelegram(101)

## `Tia.Core/FaultOb.cs` (336 linhas)
FaultObConfig(15)  FaultOb(37)  Module(44)  Generate(51)

## `Tia.Core/Hardware.cs` (542 linhas)
Hardware(13)  FindDevice(15)  AddDevice(48)  DeleteDevice(75)  PlugModule(96)  SetAddress(184)  SetIoAddress(221)  ListAttrs(274)  SetAttr(307)  SetMemoryBytes(357)  ConnectSubnet(431)  CaxExport(513)  CaxImport(526)

## `Tia.Core/Hmi.cs` (43 linhas)
Hmi(10)  Targets(12)  List(23)

## `Tia.Core/InstrumentFc.cs` (668 linhas)
InstrumentFcConfig(16)  InstrumentFc(55)  Instrument(65)  AreaTask(78)  Run(86)

## `Tia.Core/Inventory.cs` (535 linhas)
Inventory(28)  Info(30)  Devices(47)  FolderMatches(90)  Blocks(96)  Tree(150)  TagTables(223)  Types(271)  Find(294)  Snapshot(371)  Xref(424)  Trace(472)

## `Tia.Core/LadConverter.cs` (501 linhas)
LadConverter(17)  Convert(19)  ParseAll(150)  NextUid(312)  TagAccess(314)  ConstAccess(322)  Operand(332)  NewNet(339)  Compile(341)  ToFlgNet(403)

## `Tia.Core/Library.cs` (315 linhas)
Library(17)  Create(43)  List(64)  ImportMasterCopy(110)  AddMasterCopy(198)  DeleteMasterCopy(256)

## `Tia.Core/Memory.cs` (113 linhas)
Memory(20)  FreeM(22)  X(68)  B(69)  W(70)  D(71)  BOOL(76)  WORD(77)  DWORD(78)  LWORD(79)

## `Tia.Core/Multiuser.cs` (98 linhas)
Multiuser(14)  ListServerProjects(21)

## `Tia.Core/Ops.cs` (977 linhas)
Ops(19)  FindBlock(23)  ResolveFolder(87)  ResolveTagFolder(140)  ResolveTypeFolder(148)  CreateFolder(169)  DeleteFolder(195)  CreateInstanceDb(262)  DeleteBlock(282)  DeleteType(297)  ExportBlock(313)  ExportTagTable(328)  ExportType(338)  ImportBlock(370)  MoveBlock(402)  ImportTagTable(466)  AddTag(494)  DeleteTag(530)  SetTag(553)  Rename(596)  ImportType(622)  ImportSource(646)  RequireUtf8Bom(739)  XmlRootType(760)  RequireRootType(770)  EnsureCultures(804)  DiffBlock(838)  BlocksIdentical(856)  Compile(901)

## `Tia.Core/Profinet.cs` (166 linhas)
ProfinetConfig(12)  ProfinetMapping(20)  Profinet(28)  Generate(30)  BoolAddressAllocator(143)  Next(153)  Skip(161)

## `Tia.Core/Replicate.cs` (472 linhas)
ReplicateFcConfig(15)  ReplicateFc(35)  Run(37)

## `Tia.Core/Scaffold.cs` (321 linhas)
ScaffoldManifest(22)  ScaffoldItem(46)  ScaffoldPlanItem(55)  Scaffold(68)  Plan(85)  Run(135)

## `Tia.Core/Standardize.cs` (609 linhas)
StandardizeConfig(14)  PrefixMapping(128)  TagTemplate(134)  NaturalStringComparer(141)  Compare(143)  AlarmTagComparer(170)  Compare(180)  AddressAllocator(199)  Next(212)  BYTE(227)  WORD(228)  DWORD(229)  MemoryManager(238)  AllocateBlock(247)  Standardize(272)  Run(274)

## `Tia.Core/TiaSession.cs` (231 linhas)
TiaSession(12)  Attach(67)  OpenProject(89)  CreateProject(119)  Save(154)  CloseProject(161)  AllDevices(171)  ExclusiveAccess(188)  Plcs(194)  GetPlc(206)  Dispose(226)

## `Tia.Tests/Program.cs` (565 linhas)
Program(26)  Add(261)  Trail(262)

