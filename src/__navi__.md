# __navi__ · src/ (C#) — símbolos públicos por arquivo
<!-- gerado por scripts/navi-cs.ps1 · regenerar após refatorar -->

## `Tia.Cli/Program.cs` (759 linhas)
Program(16)  save-project(313)  close-project(316)  info(319)  list-devices(322)  list-blocks(325)  list-tags(329)  tree(332)  list-types(336)  find(339)  snapshot(343)  xref(346)  trace(349)  list-hmi(352)  free-memory(355)  export-block(361)  explain-block(364)  export-tags(369)  import-block(372)  import-ladder(379)  import-source(387)  create-folder(391)  delete-folder(396)  delete-block(401)  move-block(405)  delete-type(410)  export-type(414)  import-type(417)  scaffold(421)  clone(430)  add-db-member(436)  import-tags(442)  list-library(446)  import-master-copy(449)  add-master-copy(455)  create-instance-db(461)  delete-master-copy(466)  add-device(471)  delete-device(476)  add-tag(480)  delete-tag(486)  edit-db-member(491)  rename-block(497)  set-tag(502)  set-attr(509)  list-attrs(515)  plug-module(519)  list-telegrams(525)  insert-telegram(528)  set-address(535)  set-memory-bytes(541)  connect-subnet(547)  export-cax(552)  import-cax(555)  compile(559)  diff-block(574)  audit(578)  doctor(582)  gen-profinet(590)  standardize-tags(596)  gen-fault-ob(604)  replicate-fc(612)  gen-alarm-fc(619)  replicate-instruments(627)

## `Tia.Core/AlarmFc.cs` (649 linhas)
AlarmFcConfig(17)  AlarmFc(39)  Generate(55)  LEITURA_MUITO_ALTA(610)  LEITURA_ALTA(611)  LEITURA_BAIXA(612)  LEITURA_MUITO_BAIXA(613)  SEM_4MA(614)

## `Tia.Core/AssemblyInfo.cs` (3 linhas)


## `Tia.Core/Audit.cs` (215 linhas)
Audit(20)  TagOf(38)  CarriesTag(45)  NormalizeArea(52)  Run(62)

## `Tia.Core/BlockExplain.cs` (361 linhas)
BlockExplain(22)  Explain(24)  Statements(184)  Coil(207)  SCoil(208)  RCoil(209)  Move(210)  Contact(301)  Eq(304)  O(311)  A(313)  Call(315)

## `Tia.Core/Clone.cs` (174 linhas)
Clone(24)  Run(26)  RewriteFile(136)  ParseReplaces(151)

## `Tia.Core/DbMember.cs` (247 linhas)
DbMember(21)  Add(30)  Change(68)  Delta(102)  Edit(148)

## `Tia.Core/Doctor.cs` (183 linhas)
Doctor(14)  Run(23)  gen-profinet(67)  standardize-tags(84)  gen-fault-ob(93)  replicate-fc(109)  gen-alarm-fc(126)  replicate-instruments(144)

## `Tia.Core/Drives.cs` (153 linhas)
Drives(17)  ListTelegrams(62)  InsertTelegram(79)

## `Tia.Core/FaultOb.cs` (323 linhas)
FaultObConfig(15)  FaultOb(35)  Module(42)  Generate(49)

## `Tia.Core/Hardware.cs` (449 linhas)
Hardware(13)  FindDevice(15)  AddDevice(48)  DeleteDevice(75)  PlugModule(96)  SetAddress(169)  ListAttrs(205)  SetAttr(238)  SetMemoryBytes(288)  ConnectSubnet(362)  CaxExport(420)  CaxImport(433)

## `Tia.Core/Hmi.cs` (43 linhas)
Hmi(10)  Targets(12)  List(23)

## `Tia.Core/InstrumentFc.cs` (639 linhas)
InstrumentFcConfig(16)  InstrumentFc(49)  Instrument(59)  AreaTask(69)  Run(77)

## `Tia.Core/Inventory.cs` (478 linhas)
Inventory(28)  Info(30)  Devices(47)  Blocks(83)  Tree(142)  TagTables(214)  Types(234)  Find(257)  Snapshot(314)  Xref(367)  Trace(415)

## `Tia.Core/LadConverter.cs` (501 linhas)
LadConverter(17)  Convert(19)  ParseAll(150)  NextUid(312)  TagAccess(314)  ConstAccess(322)  Operand(332)  NewNet(339)  Compile(341)  ToFlgNet(403)

## `Tia.Core/Library.cs` (277 linhas)
Library(17)  List(32)  ImportMasterCopy(78)  AddMasterCopy(166)  DeleteMasterCopy(218)

## `Tia.Core/Memory.cs` (113 linhas)
Memory(20)  FreeM(22)  X(68)  B(69)  W(70)  D(71)  BOOL(76)  WORD(77)  DWORD(78)  LWORD(79)

## `Tia.Core/Multiuser.cs` (98 linhas)
Multiuser(14)  ListServerProjects(21)

## `Tia.Core/Ops.cs` (870 linhas)
Ops(18)  FindBlock(22)  ResolveFolder(67)  ResolveTagFolder(107)  ResolveTypeFolder(126)  CreateFolder(158)  DeleteFolder(184)  CreateInstanceDb(251)  DeleteBlock(271)  DeleteType(286)  ExportBlock(302)  ExportTagTable(317)  ExportType(327)  ImportBlock(348)  MoveBlock(377)  ImportTagTable(441)  AddTag(464)  DeleteTag(500)  SetTag(523)  Rename(566)  ImportType(592)  ImportSource(610)  XmlRootType(664)  RequireRootType(674)  EnsureCultures(697)  DiffBlock(731)  BlocksIdentical(749)  Compile(794)

## `Tia.Core/Profinet.cs` (166 linhas)
ProfinetConfig(12)  ProfinetMapping(20)  Profinet(28)  Generate(30)  BoolAddressAllocator(143)  Next(153)  Skip(161)

## `Tia.Core/Replicate.cs` (457 linhas)
ReplicateFcConfig(15)  ReplicateFc(35)  Run(37)

## `Tia.Core/Scaffold.cs` (321 linhas)
ScaffoldManifest(22)  ScaffoldItem(46)  ScaffoldPlanItem(55)  Scaffold(68)  Plan(85)  Run(135)

## `Tia.Core/Standardize.cs` (605 linhas)
StandardizeConfig(14)  PrefixMapping(128)  TagTemplate(134)  NaturalStringComparer(141)  Compare(143)  AlarmTagComparer(170)  Compare(180)  AddressAllocator(199)  Next(212)  BYTE(227)  WORD(228)  DWORD(229)  MemoryManager(238)  AllocateBlock(247)  Standardize(272)  Run(274)

## `Tia.Core/TiaSession.cs` (231 linhas)
TiaSession(12)  Attach(67)  OpenProject(89)  CreateProject(119)  Save(154)  CloseProject(161)  AllDevices(171)  ExclusiveAccess(188)  Plcs(194)  GetPlc(206)  Dispose(226)

## `Tia.Tests/Program.cs` (450 linhas)
Program(25)

