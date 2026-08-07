# __navi__ · src/ (C#) — símbolos públicos por arquivo
<!-- gerado por scripts/navi-cs.ps1 · regenerar após refatorar -->

## `Tia.Cli/Program.cs` (767 linhas)
Program(16)  save-project(316)  close-project(319)  info(322)  list-devices(325)  list-blocks(328)  list-tags(332)  tree(335)  list-types(339)  find(342)  snapshot(346)  xref(349)  trace(352)  list-hmi(355)  free-memory(358)  export-block(364)  explain-block(367)  export-tags(372)  import-block(375)  import-ladder(382)  import-source(390)  create-folder(394)  delete-folder(399)  delete-block(404)  move-block(408)  delete-type(413)  export-type(417)  import-type(420)  scaffold(424)  clone(433)  add-db-member(439)  import-tags(445)  list-library(449)  import-master-copy(452)  add-master-copy(458)  create-instance-db(464)  delete-master-copy(469)  add-device(474)  delete-device(479)  add-tag(483)  delete-tag(489)  edit-db-member(494)  delete-db-member(500)  rename-block(505)  set-tag(510)  set-attr(517)  list-attrs(523)  plug-module(527)  list-telegrams(533)  insert-telegram(536)  set-address(543)  set-memory-bytes(549)  connect-subnet(555)  export-cax(560)  import-cax(563)  compile(567)  diff-block(582)  audit(586)  doctor(590)  gen-profinet(598)  standardize-tags(604)  gen-fault-ob(612)  replicate-fc(620)  gen-alarm-fc(627)  replicate-instruments(635)

## `Tia.Core/AlarmFc.cs` (649 linhas)
AlarmFcConfig(17)  AlarmFc(39)  Generate(55)  LEITURA_MUITO_ALTA(610)  LEITURA_ALTA(611)  LEITURA_BAIXA(612)  LEITURA_MUITO_BAIXA(613)  SEM_4MA(614)

## `Tia.Core/AssemblyInfo.cs` (3 linhas)


## `Tia.Core/Audit.cs` (215 linhas)
Audit(20)  TagOf(38)  CarriesTag(45)  NormalizeArea(52)  Run(62)

## `Tia.Core/BlockExplain.cs` (361 linhas)
BlockExplain(22)  Explain(24)  Statements(184)  Coil(207)  SCoil(208)  RCoil(209)  Move(210)  Contact(301)  Eq(304)  O(311)  A(313)  Call(315)

## `Tia.Core/Clone.cs` (174 linhas)
Clone(24)  Run(26)  RewriteFile(136)  ParseReplaces(151)

## `Tia.Core/DbMember.cs` (302 linhas)
DbMember(24)  Add(33)  Change(71)  Remove(110)  Delta(154)  Edit(200)

## `Tia.Core/Doctor.cs` (183 linhas)
Doctor(14)  Run(23)  gen-profinet(67)  standardize-tags(84)  gen-fault-ob(93)  replicate-fc(109)  gen-alarm-fc(126)  replicate-instruments(144)

## `Tia.Core/Drives.cs` (197 linhas)
Drives(17)  ListTelegrams(80)  InsertTelegram(97)

## `Tia.Core/FaultOb.cs` (323 linhas)
FaultObConfig(15)  FaultOb(35)  Module(42)  Generate(49)

## `Tia.Core/Hardware.cs` (473 linhas)
Hardware(13)  FindDevice(15)  AddDevice(48)  DeleteDevice(75)  PlugModule(96)  SetAddress(169)  ListAttrs(205)  SetAttr(238)  SetMemoryBytes(288)  ConnectSubnet(362)  CaxExport(444)  CaxImport(457)

## `Tia.Core/Hmi.cs` (43 linhas)
Hmi(10)  Targets(12)  List(23)

## `Tia.Core/InstrumentFc.cs` (639 linhas)
InstrumentFcConfig(16)  InstrumentFc(49)  Instrument(59)  AreaTask(69)  Run(77)

## `Tia.Core/Inventory.cs` (478 linhas)
Inventory(28)  Info(30)  Devices(47)  Blocks(83)  Tree(142)  TagTables(214)  Types(234)  Find(257)  Snapshot(314)  Xref(367)  Trace(415)

## `Tia.Core/LadConverter.cs` (501 linhas)
LadConverter(17)  Convert(19)  ParseAll(150)  NextUid(312)  TagAccess(314)  ConstAccess(322)  Operand(332)  NewNet(339)  Compile(341)  ToFlgNet(403)

## `Tia.Core/Library.cs` (293 linhas)
Library(17)  List(42)  ImportMasterCopy(88)  AddMasterCopy(176)  DeleteMasterCopy(234)

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

## `Tia.Tests/Program.cs` (461 linhas)
Program(25)

