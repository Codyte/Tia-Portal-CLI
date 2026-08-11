# __navi__ · src/ (C#) — símbolos públicos por arquivo
<!-- gerado por scripts/navi-cs.ps1 · regenerar após refatorar -->

## `Tia.Cli/Program.cs` (921 linhas)
Program(17)  save-project(395)  close-project(398)  info(401)  list-devices(404)  list-blocks(407)  list-tags(411)  tree(414)  list-types(418)  find(421)  snapshot(425)  xref(428)  trace(431)  list-hmi(434)  free-memory(437)  export-block(443)  explain-block(446)  export-tags(451)  list-interface(454)  import-block(460)  import-ladder(467)  import-source(475)  create-folder(480)  delete-folder(485)  delete-block(490)  move-block(494)  delete-type(499)  export-type(503)  import-type(506)  scaffold(510)  clone(519)  add-call(526)  delete-network(533)  set-retain(538)  add-db-member(543)  import-tags(549)  create-library(556)  list-library(560)  import-master-copy(563)  add-master-copy(569)  create-instance-db(575)  delete-master-copy(580)  add-device(585)  delete-device(590)  add-tag(594)  delete-tag(600)  edit-db-member(605)  delete-db-member(611)  rename-block(616)  set-tag(621)  set-attr(628)  list-attrs(634)  plug-module(638)  list-telegrams(644)  insert-telegram(647)  set-address(654)  set-io-address(660)  set-memory-bytes(666)  connect-subnet(672)  export-cax(677)  import-cax(680)  compile(684)  diff-block(699)  audit(703)  doctor(707)  gen-profinet(715)  standardize-tags(721)  gen-fault-ob(729)  replicate-fc(737)  gen-alarm-fc(744)  replicate-instruments(752)

## `Tia.Core/AlarmFc.cs` (687 linhas)
AlarmFcConfig(17)  AlarmFc(42)  Generate(58)  LEITURA_MUITO_ALTA(648)  LEITURA_ALTA(649)  LEITURA_BAIXA(650)  LEITURA_MUITO_BAIXA(651)  SEM_4MA(652)

## `Tia.Core/AssemblyInfo.cs` (3 linhas)


## `Tia.Core/Audit.cs` (249 linhas)
Audit(20)  TagOf(61)  CarriesTag(68)  NormalizeArea(75)  Run(85)

## `Tia.Core/BlockEdit.cs` (413 linhas)
BlockEdit(29)  DeleteNetwork(44)  AddCall(67)  SetRetain(129)  CallSpec(185)

## `Tia.Core/BlockExplain.cs` (361 linhas)
BlockExplain(22)  Explain(24)  Statements(184)  Coil(207)  SCoil(208)  RCoil(209)  Move(210)  Contact(301)  Eq(304)  O(311)  A(313)  Call(315)

## `Tia.Core/BlockInterface.cs` (141 linhas)
Param(17)  ToString(23)  BlockInterface(32)  Run(37)  FromXml(97)  Describe(117)

## `Tia.Core/Clone.cs` (230 linhas)
Clone(24)  Run(26)  RewriteFile(192)  ParseReplaces(207)

## `Tia.Core/DbMember.cs` (351 linhas)
DbMember(27)  Add(36)  Change(82)  Remove(128)  Delta(203)  Edit(249)

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
Inventory(28)  Info(30)  Devices(47)  FolderMatches(86)  Blocks(96)  Tree(150)  TagTables(223)  Types(271)  Find(294)  Snapshot(371)  Xref(424)  Trace(472)

## `Tia.Core/LadConverter.cs` (501 linhas)
LadConverter(17)  Convert(19)  ParseAll(150)  NextUid(312)  TagAccess(314)  ConstAccess(322)  Operand(332)  NewNet(339)  Compile(341)  ToFlgNet(403)

## `Tia.Core/Library.cs` (315 linhas)
Library(17)  Create(43)  List(64)  ImportMasterCopy(110)  AddMasterCopy(198)  DeleteMasterCopy(256)

## `Tia.Core/Memory.cs` (113 linhas)
Memory(20)  FreeM(22)  X(68)  B(69)  W(70)  D(71)  BOOL(76)  WORD(77)  DWORD(78)  LWORD(79)

## `Tia.Core/Multiuser.cs` (98 linhas)
Multiuser(14)  ListServerProjects(21)

## `Tia.Core/Ops.cs` (1101 linhas)
Ops(20)  FindBlock(24)  ResolveFolder(108)  ResolveTagFolder(161)  ResolveTypeFolder(169)  CreateFolder(190)  DeleteFolder(216)  CreateInstanceDb(283)  DeleteBlock(353)  DeleteType(368)  ExportBlock(384)  ExportTagTable(399)  ExportType(409)  ImportBlock(441)  MoveBlock(473)  ImportTagTable(537)  AddTag(565)  DeleteTag(601)  SetTag(624)  Rename(667)  ImportType(693)  ImportSource(717)  RequireUtf8Bom(810)  XmlRootType(831)  RequireRootType(841)  EnsureCultures(875)  DiffBlock(909)  BlocksIdentical(927)  Compile(1025)

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

## `Tia.Tests/Program.cs` (736 linhas)
Program(26)  Add(268)  Trail(269)

