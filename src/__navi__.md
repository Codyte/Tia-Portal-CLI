# __navi__ · src/ (C#) — símbolos públicos por arquivo
<!-- gerado por scripts/navi-cs.ps1 · regenerar após refatorar -->

## `Tia.Cli/Program.cs` (777 linhas)
Program(16)  save-project(319)  close-project(322)  info(325)  list-devices(328)  list-blocks(331)  list-tags(335)  tree(338)  list-types(342)  find(345)  snapshot(349)  xref(352)  trace(355)  list-hmi(358)  free-memory(361)  export-block(367)  explain-block(370)  export-tags(375)  import-block(378)  import-ladder(385)  import-source(393)  create-folder(398)  delete-folder(403)  delete-block(408)  move-block(412)  delete-type(417)  export-type(421)  import-type(424)  scaffold(428)  clone(437)  add-db-member(443)  import-tags(449)  list-library(453)  import-master-copy(456)  add-master-copy(462)  create-instance-db(468)  delete-master-copy(473)  add-device(478)  delete-device(483)  add-tag(487)  delete-tag(493)  edit-db-member(498)  delete-db-member(504)  rename-block(509)  set-tag(514)  set-attr(521)  list-attrs(527)  plug-module(531)  list-telegrams(537)  insert-telegram(540)  set-address(547)  set-io-address(553)  set-memory-bytes(559)  connect-subnet(565)  export-cax(570)  import-cax(573)  compile(577)  diff-block(592)  audit(596)  doctor(600)  gen-profinet(608)  standardize-tags(614)  gen-fault-ob(622)  replicate-fc(630)  gen-alarm-fc(637)  replicate-instruments(645)

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

## `Tia.Core/Inventory.cs` (480 linhas)
Inventory(28)  Info(30)  Devices(47)  Blocks(83)  Tree(144)  TagTables(216)  Types(236)  Find(259)  Snapshot(316)  Xref(369)  Trace(417)

## `Tia.Core/LadConverter.cs` (501 linhas)
LadConverter(17)  Convert(19)  ParseAll(150)  NextUid(312)  TagAccess(314)  ConstAccess(322)  Operand(332)  NewNet(339)  Compile(341)  ToFlgNet(403)

## `Tia.Core/Library.cs` (293 linhas)
Library(17)  List(42)  ImportMasterCopy(88)  AddMasterCopy(176)  DeleteMasterCopy(234)

## `Tia.Core/Memory.cs` (113 linhas)
Memory(20)  FreeM(22)  X(68)  B(69)  W(70)  D(71)  BOOL(76)  WORD(77)  DWORD(78)  LWORD(79)

## `Tia.Core/Multiuser.cs` (98 linhas)
Multiuser(14)  ListServerProjects(21)

## `Tia.Core/Ops.cs` (915 linhas)
Ops(19)  FindBlock(23)  ResolveFolder(68)  ResolveTagFolder(119)  ResolveTypeFolder(127)  CreateFolder(148)  DeleteFolder(174)  CreateInstanceDb(241)  DeleteBlock(261)  DeleteType(276)  ExportBlock(292)  ExportTagTable(307)  ExportType(317)  ImportBlock(338)  MoveBlock(368)  ImportTagTable(432)  AddTag(458)  DeleteTag(494)  SetTag(517)  Rename(560)  ImportType(586)  ImportSource(610)  XmlRootType(698)  RequireRootType(708)  EnsureCultures(742)  DiffBlock(776)  BlocksIdentical(794)  Compile(839)

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

## `Tia.Tests/Program.cs` (469 linhas)
Program(25)

