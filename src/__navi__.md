# __navi__ · src/ (C#) — símbolos públicos por arquivo
<!-- gerado por scripts/navi-cs.ps1 · regenerar após refatorar -->

## `Tia.Cli/Program.cs` (802 linhas)
Program(16)  save-project(340)  close-project(343)  info(346)  list-devices(349)  list-blocks(352)  list-tags(356)  tree(359)  list-types(363)  find(366)  snapshot(370)  xref(373)  trace(376)  list-hmi(379)  free-memory(382)  export-block(388)  explain-block(391)  export-tags(396)  import-block(399)  import-ladder(406)  import-source(414)  create-folder(419)  delete-folder(424)  delete-block(429)  move-block(433)  delete-type(438)  export-type(442)  import-type(445)  scaffold(449)  clone(458)  add-db-member(464)  import-tags(470)  create-library(474)  list-library(478)  import-master-copy(481)  add-master-copy(487)  create-instance-db(493)  delete-master-copy(498)  add-device(503)  delete-device(508)  add-tag(512)  delete-tag(518)  edit-db-member(523)  delete-db-member(529)  rename-block(534)  set-tag(539)  set-attr(546)  list-attrs(552)  plug-module(556)  list-telegrams(562)  insert-telegram(565)  set-address(572)  set-io-address(578)  set-memory-bytes(584)  connect-subnet(590)  export-cax(595)  import-cax(598)  compile(602)  diff-block(617)  audit(621)  doctor(625)  gen-profinet(633)  standardize-tags(639)  gen-fault-ob(647)  replicate-fc(655)  gen-alarm-fc(662)  replicate-instruments(670)

## `Tia.Core/AlarmFc.cs` (671 linhas)
AlarmFcConfig(17)  AlarmFc(42)  Generate(58)  LEITURA_MUITO_ALTA(632)  LEITURA_ALTA(633)  LEITURA_BAIXA(634)  LEITURA_MUITO_BAIXA(635)  SEM_4MA(636)

## `Tia.Core/AssemblyInfo.cs` (3 linhas)


## `Tia.Core/Audit.cs` (215 linhas)
Audit(20)  TagOf(38)  CarriesTag(45)  NormalizeArea(52)  Run(62)

## `Tia.Core/BlockExplain.cs` (361 linhas)
BlockExplain(22)  Explain(24)  Statements(184)  Coil(207)  SCoil(208)  RCoil(209)  Move(210)  Contact(301)  Eq(304)  O(311)  A(313)  Call(315)

## `Tia.Core/Clone.cs` (198 linhas)
Clone(24)  Run(26)  RewriteFile(160)  ParseReplaces(175)

## `Tia.Core/DbMember.cs` (302 linhas)
DbMember(24)  Add(33)  Change(71)  Remove(110)  Delta(154)  Edit(200)

## `Tia.Core/Doctor.cs` (183 linhas)
Doctor(14)  Run(23)  gen-profinet(67)  standardize-tags(84)  gen-fault-ob(93)  replicate-fc(109)  gen-alarm-fc(126)  replicate-instruments(144)

## `Tia.Core/Drives.cs` (201 linhas)
Drives(17)  ListTelegrams(84)  InsertTelegram(101)

## `Tia.Core/FaultOb.cs` (336 linhas)
FaultObConfig(15)  FaultOb(37)  Module(44)  Generate(51)

## `Tia.Core/Hardware.cs` (527 linhas)
Hardware(13)  FindDevice(15)  AddDevice(48)  DeleteDevice(75)  PlugModule(96)  SetAddress(169)  SetIoAddress(206)  ListAttrs(259)  SetAttr(292)  SetMemoryBytes(342)  ConnectSubnet(416)  CaxExport(498)  CaxImport(511)

## `Tia.Core/Hmi.cs` (43 linhas)
Hmi(10)  Targets(12)  List(23)

## `Tia.Core/InstrumentFc.cs` (668 linhas)
InstrumentFcConfig(16)  InstrumentFc(55)  Instrument(65)  AreaTask(78)  Run(86)

## `Tia.Core/Inventory.cs` (486 linhas)
Inventory(28)  Info(30)  Devices(47)  FolderMatches(90)  Blocks(96)  Tree(150)  TagTables(222)  Types(242)  Find(265)  Snapshot(322)  Xref(375)  Trace(423)

## `Tia.Core/LadConverter.cs` (501 linhas)
LadConverter(17)  Convert(19)  ParseAll(150)  NextUid(312)  TagAccess(314)  ConstAccess(322)  Operand(332)  NewNet(339)  Compile(341)  ToFlgNet(403)

## `Tia.Core/Library.cs` (315 linhas)
Library(17)  Create(43)  List(64)  ImportMasterCopy(110)  AddMasterCopy(198)  DeleteMasterCopy(256)

## `Tia.Core/Memory.cs` (113 linhas)
Memory(20)  FreeM(22)  X(68)  B(69)  W(70)  D(71)  BOOL(76)  WORD(77)  DWORD(78)  LWORD(79)

## `Tia.Core/Multiuser.cs` (98 linhas)
Multiuser(14)  ListServerProjects(21)

## `Tia.Core/Ops.cs` (958 linhas)
Ops(19)  FindBlock(23)  ResolveFolder(68)  ResolveTagFolder(121)  ResolveTypeFolder(129)  CreateFolder(150)  DeleteFolder(176)  CreateInstanceDb(243)  DeleteBlock(263)  DeleteType(278)  ExportBlock(294)  ExportTagTable(309)  ExportType(319)  ImportBlock(351)  MoveBlock(383)  ImportTagTable(447)  AddTag(475)  DeleteTag(511)  SetTag(534)  Rename(577)  ImportType(603)  ImportSource(627)  RequireUtf8Bom(720)  XmlRootType(741)  RequireRootType(751)  EnsureCultures(785)  DiffBlock(819)  BlocksIdentical(837)  Compile(882)

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

