# __navi__ · src/ (C#) — símbolos públicos por arquivo
<!-- gerado por scripts/navi-cs.ps1 · regenerar após refatorar -->

## `Tia.Cli/Program.cs` (899 linhas)
Program(17)  save-project(377)  close-project(380)  info(383)  list-devices(386)  list-blocks(389)  list-tags(393)  tree(396)  list-types(400)  find(403)  snapshot(407)  xref(410)  trace(413)  list-hmi(416)  free-memory(419)  export-block(425)  explain-block(428)  export-tags(433)  list-interface(436)  import-block(442)  import-ladder(449)  import-source(457)  create-folder(462)  delete-folder(467)  delete-block(472)  move-block(476)  delete-type(481)  export-type(485)  import-type(488)  scaffold(492)  clone(501)  add-call(508)  delete-network(515)  set-retain(520)  add-db-member(525)  import-tags(531)  create-library(538)  list-library(542)  import-master-copy(545)  add-master-copy(551)  create-instance-db(557)  delete-master-copy(562)  add-device(567)  delete-device(572)  add-tag(576)  delete-tag(582)  edit-db-member(587)  delete-db-member(593)  rename-block(598)  set-tag(603)  set-attr(610)  list-attrs(616)  plug-module(620)  list-telegrams(626)  insert-telegram(629)  set-address(636)  set-io-address(642)  set-memory-bytes(648)  connect-subnet(654)  export-cax(659)  import-cax(662)  compile(666)  diff-block(681)  audit(685)  doctor(689)  gen-profinet(697)  standardize-tags(703)  gen-fault-ob(711)  replicate-fc(719)  gen-alarm-fc(726)  replicate-instruments(734)

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

