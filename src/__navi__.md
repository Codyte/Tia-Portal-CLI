# __navi__ · src/ (C#) — símbolos públicos por arquivo
<!-- gerado por scripts/navi-cs.ps1 · regenerar após refatorar -->

## `Tia.Cli/Program.cs` (929 linhas)
Program(17)  save-project(398)  close-project(401)  info(404)  list-devices(407)  list-blocks(410)  list-tags(414)  tree(417)  list-types(421)  find(424)  snapshot(428)  xref(431)  trace(434)  list-hmi(437)  free-memory(440)  export-block(446)  explain-block(449)  export-tags(454)  list-interface(457)  import-block(463)  import-ladder(470)  import-source(478)  create-folder(483)  delete-folder(488)  delete-block(493)  move-block(497)  delete-type(502)  export-type(506)  import-type(509)  scaffold(513)  clone(522)  add-call(529)  delete-network(536)  set-retain(541)  add-db-member(546)  import-tags(552)  create-library(559)  list-library(563)  import-master-copy(566)  add-master-copy(572)  create-instance-db(578)  delete-master-copy(583)  add-device(588)  delete-device(593)  add-tag(597)  delete-tag(603)  edit-db-member(608)  delete-db-member(614)  rename-block(619)  set-tag(624)  set-attr(631)  list-attrs(637)  plug-module(641)  list-telegrams(647)  insert-telegram(650)  set-address(657)  set-io-address(663)  list-io-map(669)  set-memory-bytes(673)  connect-subnet(679)  export-cax(684)  import-cax(687)  compile(691)  diff-block(706)  audit(710)  doctor(715)  gen-profinet(723)  standardize-tags(729)  gen-fault-ob(737)  replicate-fc(745)  gen-alarm-fc(752)  replicate-instruments(760)

## `Tia.Core/AlarmFc.cs` (687 linhas)
AlarmFcConfig(17)  AlarmFc(42)  Generate(58)  LEITURA_MUITO_ALTA(648)  LEITURA_ALTA(649)  LEITURA_BAIXA(650)  LEITURA_MUITO_BAIXA(651)  SEM_4MA(652)

## `Tia.Core/AssemblyInfo.cs` (3 linhas)


## `Tia.Core/Audit.cs` (409 linhas)
Audit(22)  TagOf(63)  CarriesTag(70)  NormalizeArea(77)  Run(125)

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

## `Tia.Core/Hardware.cs` (617 linhas)
Hardware(13)  FindDevice(15)  AddDevice(48)  DeleteDevice(75)  PlugModule(96)  SetAddress(184)  SetIoAddress(221)  ListIoMap(277)  ListAttrs(343)  SetAttr(376)  SetMemoryBytes(432)  ConnectSubnet(506)  CaxExport(588)  CaxImport(601)

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

## `Tia.Core/Ops.cs` (1165 linhas)
Ops(20)  FindBlock(24)  ResolveFolder(108)  SplitPath(120)  ResolveTagFolder(184)  ResolveTypeFolder(192)  CreateFolder(213)  CreateFolders(245)  DeleteFolder(280)  CreateInstanceDb(347)  DeleteBlock(417)  DeleteType(432)  ExportBlock(448)  ExportTagTable(463)  ExportType(473)  ImportBlock(505)  MoveBlock(537)  ImportTagTable(601)  AddTag(629)  DeleteTag(665)  SetTag(688)  Rename(731)  ImportType(757)  ImportSource(781)  RequireUtf8Bom(874)  XmlRootType(895)  RequireRootType(905)  EnsureCultures(939)  DiffBlock(973)  BlocksIdentical(991)  Compile(1089)

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

## `Tia.Tests/Program.cs` (789 linhas)
Program(26)  Add(270)  Trail(271)

