# __navi__ · src/ (C#) — símbolos públicos por arquivo
<!-- gerado por scripts/navi-cs.ps1 · regenerar após refatorar -->

## `Tia.Cli/Program.cs` (847 linhas)
Program(16)  save-project(349)  close-project(352)  info(355)  list-devices(358)  list-blocks(361)  list-tags(365)  tree(368)  list-types(372)  find(375)  snapshot(379)  xref(382)  trace(385)  list-hmi(388)  free-memory(391)  export-block(397)  explain-block(400)  export-tags(405)  import-block(408)  import-ladder(415)  import-source(423)  create-folder(428)  delete-folder(433)  delete-block(438)  move-block(442)  delete-type(447)  export-type(451)  import-type(454)  scaffold(458)  clone(467)  add-db-member(473)  import-tags(479)  create-library(486)  list-library(490)  import-master-copy(493)  add-master-copy(499)  create-instance-db(505)  delete-master-copy(510)  add-device(515)  delete-device(520)  add-tag(524)  delete-tag(530)  edit-db-member(535)  delete-db-member(541)  rename-block(546)  set-tag(551)  set-attr(558)  list-attrs(564)  plug-module(568)  list-telegrams(574)  insert-telegram(577)  set-address(584)  set-io-address(590)  set-memory-bytes(596)  connect-subnet(602)  export-cax(607)  import-cax(610)  compile(614)  diff-block(629)  audit(633)  doctor(637)  gen-profinet(645)  standardize-tags(651)  gen-fault-ob(659)  replicate-fc(667)  gen-alarm-fc(674)  replicate-instruments(682)

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

## `Tia.Core/Hardware.cs` (548 linhas)
Hardware(13)  FindDevice(15)  AddDevice(48)  DeleteDevice(75)  PlugModule(96)  SetAddress(184)  SetIoAddress(221)  ListAttrs(274)  SetAttr(307)  SetMemoryBytes(363)  ConnectSubnet(437)  CaxExport(519)  CaxImport(532)

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

## `Tia.Core/Ops.cs` (997 linhas)
Ops(19)  FindBlock(23)  ResolveFolder(107)  ResolveTagFolder(160)  ResolveTypeFolder(168)  CreateFolder(189)  DeleteFolder(215)  CreateInstanceDb(282)  DeleteBlock(302)  DeleteType(317)  ExportBlock(333)  ExportTagTable(348)  ExportType(358)  ImportBlock(390)  MoveBlock(422)  ImportTagTable(486)  AddTag(514)  DeleteTag(550)  SetTag(573)  Rename(616)  ImportType(642)  ImportSource(666)  RequireUtf8Bom(759)  XmlRootType(780)  RequireRootType(790)  EnsureCultures(824)  DiffBlock(858)  BlocksIdentical(876)  Compile(921)

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

