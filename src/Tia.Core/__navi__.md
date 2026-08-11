# __navi__ · `src/Tia.Core/` — 25 files → symbols at exact line numbers
<!-- navindex · 2026-08-11 · DO NOT EDIT BY HAND; regen via navindex skill -->
↑ repo tree: [`../../__navi__.md`](../../__navi__.md)

- **AlarmFc.cs** (728 ln)
  <sub>L58:class AlarmFcConfig  L60:.TargetRootFolder  L61:.TemplateFc  L62:.TemplateFolder  L63:.ObTemplate  L64:.GlobalDb  L65:.AlarmTagsFolder  L66:.StartTagsFolder  L67:.MasterFb  L68:.CallObName  L69:.CallObNumber  L70:.IgnoreFolders  L72:.Structs  L83:class AlarmFc  L93:class TagRef  L99:.Generate  L317:FC XML  L319:.BuildFcXml  L356:.RewireWordNetwork  L465:call OB  L467:.BuildCallObXml  L517:global DB comments  L519:.WriteDbComments  L549:.FindParentStruct …</sub>
- **AssemblyInfo.cs** (3 ln)
- **Audit.cs** (436 ln)
  <sub>L49:class Audit  L75:.HasInverter  L82:.MissingCore  L90:.TagOf  L97:.CarriesTag  L104:.NormalizeArea  L120:.IsCallBlock  L130:.IsLooseScalar  L141:.RootMembers  L152:.Run  L237:.NonGraphicCalls  L257:.MisplacedCalls  L283:.DbGlobalCheck  L303:.FindGlobalDb  L317:.Skipped  L326:.CountTypes  L331:.CollectLanguages  L344:.LayerLeaks  L370:.IsLibrary  L377:.AreaConflicts  L404:.Check  L420:.CollectBlocks  L428:.CollectTables</sub>
- **BlockEdit.cs** (486 ln)
  <sub>L58:class BlockEdit  L76:delete-network  L82:.DeleteNetwork  L96:add-call  L108:.AddCall  L173:set-retain  L181:.SetRetain  L203:coreografia  L209:.Patch  L235:núcleo puro (sem Openness: testável offline)  L237:class CallSpec  L249:.CountNetworks  L255:.RemoveNetworkFromXml  L275:.InsertCallInXml  L361:.SetRetainInXml  L369:.RetainOf  L375:.FindMember  L393:helpers de FlgNet  L395:.ParseParams  L408:.Access  L438:.Wire  L447:.Text  L462:.NextId  L475:.Escape …</sub>
- **BlockExplain.cs** (381 ln)
  <sub>L42:class BlockExplain  L44:.Explain  L84:.Val  L91:.Kids  L97:interface / membros de DB  L100:.Interface  L116:.Members  L125:.Member  L139:rede  L141:.Network  L160:.Text  L172:.Collapse  L173:.Cut  L175:FlgNet → expressão  L177:class Net  L362:rótulo do operando</sub>
- **BlockInterface.cs** (141 ln)
  <sub>L17:class Param  L23:.ToString  L32:class BlockInterface  L37:.Run  L88:.Collect  L94:núcleo puro (sem Openness: testável offline)  L97:.FromXml  L117:.Describe</sub>
- **Clone.cs** (230 ln)
  <sub>L24:class Clone  L26:.Run  L95:.InstancesInXml  L113:.Rewrite  L141:.Readdress  L192:.RewriteFile  L207:.ParseReplaces  L220:.ObjectName</sub>
- **DbMember.cs** (372 ln)
  <sub>L48:class DbMember  L57:.Add  L103:.Change  L149:.Remove  L179:coreografia comum: export → patch → Import Override → prova  L187:.ExportFresh  L199:.MemberOf  L211:.RemoveFromXml  L224:struct Delta  L232:.ChangeInXml  L270:struct Edit  L281:.AddToXml  L311:.ResolveSection  L338:.NameOf  L344:.Datatype  L352:.Safe  L357:.Report</sub>
- **Doctor.cs** (183 ln)
  <sub>L14:class Doctor  L23:.Run  L43:.CheckVerb  L67:case "gen-profinet"  L84:case "standardize-tags"  L93:case "gen-fault-ob"  L109:case "replicate-fc"  L126:case "gen-alarm-fc"  L144:case "replicate-instruments"  L175:.AnyFc</sub>
- **Drives.cs** (234 ln)
  <sub>L31:class Drives  L34:.DriveObjects  L41:.Collect  L61:.Try  L67:.Describe  L115:list-telegrams  L117:.ListTelegrams  L128:insert-telegram  L134:.InsertTelegram  L223:.ParseType</sub>
- **FaultOb.cs** (361 ln)
  <sub>L40:class FaultObConfig  L43:.GroupPrefix  L45:.Devices  L46:.TemplateOb  L47:.ObNamePrefix  L53:.AlarmDb  L54:.CommentCultures  L62:class FaultOb  L69:class Module  L76:.Generate  L156:.DiscoverTasks  L185:.AllDeviceGroups  L192:.WithSubGroups  L200:.CollectModules  L211:XML generation  L213:.BuildObXml  L243:.RewireNetwork  L280:.AddMasterCommentEntry  L309:.ReassignUids  L329:.ModuleType  L341:.WriteCsv</sub>
- **Hardware.cs** (760 ln)
  <sub>L56:class Hardware  L62:.FindDevice  L74:.HasItemNamed  L84:.Interface  L92:add-device  L95:.AddDevice  L120:delete-device  L122:.DeleteDevice  L135:plug-module  L143:.PlugModule  L212:.CollectSlots  L224:.FindItem  L237:.FindItem  L248:set-address  L250:.SetAddress  L279:set-io-address  L287:.SetIoAddress  L327:.CollectAddresses  L333:list-io-map  L343:.ListIoMap  L384:.CollectMap  L412:.CollectTelegramMap  L439:.Range  L445:list-attrs …</sub>
- **Hmi.cs** (43 ln)
  <sub>L10:class Hmi  L12:.Targets  L23:.List</sub>
- **InstrumentFc.cs** (706 ln)
  <sub>L54:class InstrumentFcConfig  L57:.SourceTagsFolder  L59:.TargetBlocksFolder  L60:.GlobalDb  L62:.TargetOb  L69:.FcSuffix  L70:.IgnoreFolders  L72:.TagFilters  L78:.MoldInstrumentId  L80:.NextCommandIds  L93:class InstrumentFc  L103:class Instrument  L116:class AreaTask  L124:.Run  L323:generation  L325:.BuildAreaFcXml  L353:.RewireNetwork  L407:.ImportAreaFc  L447:call OB  L449:.UpdateCallOb  L513:.CallNetworkXml  L544:.EmptyObXml  L574:checks + helpers  L577:.IsTaskComplete …</sub>
- **Inventory.cs** (572 ln)
  <sub>L61:class Inventory  L63:.Info  L80:.Devices  L97:.CollectDeviceItems  L119:.FolderMatches  L133:.Blocks  L164:.CollectBlocks  L187:.Tree  L219:.AppendGrouped  L231:.AppendTree  L252:.BlockLabel  L260:.TagTables  L283:.FindTagTable  L295:.CollectTagTables  L308:.Types  L315:.CollectTypes  L328:find  L331:.Find  L365:.FindInTagTables  L405:snapshot  L408:.Snapshot  L426:cross-references  L433:.ResolveSymbol  L447:.FindTag …</sub>
- **LadConverter.cs** (530 ln)
  <sub>L46:class LadConverter  L48:.Convert  L91:lexer  L93:class Tok  L99:.Lex  L143:AST  L145:class Node  L146:class Leaf  L147:class CmpN  L148:class Group  L149:class NotN  L150:class Operand  L153:class Stmt  L161:parser (recursive descent)  L163:class Parser  L304:normalize: push NOT down to leaves (De Morgan; comparators invert)  L306:.Normalize  L321:FlgNet emitter  L327:class Net  L334:class Emitter  L450:.EmitNetwork  L476:block XML assembly  L478:.BuildBlockXml  L516:.Hex …</sub>
- **Library.cs** (331 ln)
  <sub>L33:class Library  L35:.Open  L59:.Create  L80:.List  L95:.CollectMasterCopies  L108:.CollectTypes  L126:.ImportMasterCopy  L214:.AddMasterCopy  L272:.DeleteMasterCopy  L291:.ResolveLibFolder  L304:.FindMasterCopy  L322:.Collect</sub>
- **Memory.cs** (113 ln)
  <sub>L20:class Memory  L22:.FreeM  L47:.Occupied  L64:.Width  L85:.Gaps  L104:.CollectTags</sub>
- **Multiuser.cs** (98 ln)
  <sub>L14:class Multiuser  L21:.ListServerProjects  L54:.ResolveServer  L69:.Describe</sub>
- **Ops.cs** (1260 ln)
  <sub>L91:class Ops  L93:lookup  L95:.FindBlock  L105:.FindGroup  L120:.FindGroupByName  L138:.FindTagGroup  L153:.FindTagGroupByName  L166:.FindBlockIn  L179:.ResolveFolder  L191:.SplitPath  L215:.WalkFolders  L242:.FindTagTable  L255:.ResolveTagFolder  L263:.ResolveTypeFolder  L270:.FindType  L282:structure  L284:.CreateFolder  L316:.CreateFolders  L351:.DeleteFolder  L378:.TypeFolderAction  L399:.CountTypes  L404:.CountBlocks  L409:.CountTables  L418:.CreateInstanceDb …</sub>
- **Profinet.cs** (166 ln)
  <sub>L12:class ProfinetConfig  L14:.Devices  L15:.StartByte  L16:.TagFolder  L17:.TagTable  L20:class ProfinetMapping  L22:.Hardware  L23:.EquipmentTag  L24:.DeviceNumber  L28:class Profinet  L30:.Generate  L92:.TagName  L97:.FindTable  L103:.ResolveTable  L111:.FindIoDeviceNames  L130:.FindNetworkItem  L143:class BoolAddressAllocator  L148:.BoolAddressAllocator  L153:.Next  L161:.Skip</sub>
- **Replicate.cs** (500 ln)
  <sub>L43:class ReplicateFcConfig  L46:.BlocksFolder  L48:.EquipmentTypes  L50:.UdtNames  L52:.SourceNumbersToReplace  L53:.GlobalDb  L55:.StartNumber  L63:class ReplicateFc  L65:.Run  L209:.FoldersOfType  L218:.ReplicateInto  L266:.RewireXml  L361:.FindPathInDbXml  L395:naming  L397:.ExtractId  L403:.InstanceDbNames  L408:.ProposedBlockName  L421:.MainBlockName  L428:.FolderBaseName  L436:.static  L454:lookups  L456:.DescendantGroups  L470:.FindDataBlock  L483:.FindTag</sub>
- **Scaffold.cs** (350 ln)
  <sub>L51:class ScaffoldManifest  L54:.Source  L57:.Folders  L60:.TagFolders  L63:.Replace  L70:.Cpu  L72:.Items  L75:class ScaffoldItem  L77:.File  L80:.Folder  L84:class ScaffoldPlanItem  L97:class Scaffold  L100:.Rank  L114:.Plan  L145:.Merge  L155:.Apply  L164:.Run  L238:.CheckFamily  L252:.SameFamily  L259:.AlreadyInAnotherFolder  L267:.DeleteObject  L278:.FolderAction  L301:.ResolveBlockPath  L318:.ResolveTypePath …</sub>
- **Standardize.cs** (654 ln)
  <sub>L59:class StandardizeConfig  L61:.RootFolder  L64:.MemorySets  L72:.SetMapping  L83:.PrefixMappings  L102:.CommentMappings  L167:.AlarmOrder  L173:class PrefixMapping  L175:.Keyword  L176:.Prefix  L179:class TagTemplate  L186:class NaturalStringComparer  L188:.Compare  L215:class AlarmTagComparer  L220:.AlarmTagComparer  L225:.Compare  L233:.Key  L244:class AddressAllocator  L248:.CurrentByte  L249:.CurrentBit  L251:.AddressAllocator  L257:.Next  L283:class MemoryManager  L287:.MemoryManager …</sub>
- **TiaSession.cs** (231 ln)
  <sub>L12:class TiaSession  L14:.Portal  L15:.Project  L17:.TiaSession  L24:.PortalFilter  L30:.PickProcess  L61:.Describe  L67:.Attach  L82:lifecycle (open/save/close)  L89:.OpenProject  L119:.CreateProject  L145:.LocalProject  L154:.Save  L161:.CloseProject  L171:.AllDevices  L180:.CollectDevices  L188:.ExclusiveAccess  L194:.Plcs  L206:.GetPlc  L226:.Dispose</sub>

