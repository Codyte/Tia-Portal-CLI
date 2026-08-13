# __navi__ · `src/Tia.Core/` — 25 files → symbols at exact line numbers
<!-- navindex · 2026-08-13 · DO NOT EDIT BY HAND; regen via navindex skill -->
↑ repo tree: [`../../__navi__.md`](../../__navi__.md)

- **AlarmFc.cs** (750 ln)
  <sub>L59:class AlarmFcConfig  L61:.TargetRootFolder  L62:.TemplateFc  L63:.TemplateFolder  L64:.ObTemplate  L65:.GlobalDb  L66:.AlarmTagsFolder  L67:.StartTagsFolder  L68:.MasterFb  L69:.CallObName  L70:.CallObNumber  L71:.IgnoreFolders  L78:.IncludeFolders  L80:.Structs  L91:class AlarmFc  L101:class TagRef  L107:.Generate  L340:FC XML  L342:.BuildFcXml  L379:.RewireWordNetwork  L488:call OB  L490:.BuildCallObXml  L540:global DB comments  L542:.WriteDbComments …</sub>
- **AssemblyInfo.cs** (3 ln)
- **Audit.cs** (449 ln)
  <sub>L49:class Audit  L75:.HasInverter  L82:.MissingCore  L90:.TagOf  L97:.CarriesTag  L104:.NormalizeArea  L120:.IsCallBlock  L130:.IsLooseScalar  L141:.RootMembers  L152:.Run  L250:.NonGraphicCalls  L270:.MisplacedCalls  L296:.DbGlobalCheck  L316:.FindGlobalDb  L330:.Skipped  L339:.CountTypes  L344:.CollectLanguages  L357:.LayerLeaks  L383:.IsLibrary  L390:.AreaConflicts  L417:.Check  L433:.CollectBlocks  L441:.CollectTables</sub>
- **BlockEdit.cs** (523 ln)
  <sub>L60:class BlockEdit  L78:delete-network  L84:.DeleteNetwork  L100:add-call  L113:.AddCall  L199:set-retain  L207:.SetRetain  L229:coreografia  L235:.Patch  L268:núcleo puro (sem Openness: testável offline)  L270:class CallSpec  L283:.StripTypePrefix  L288:.CountNetworks  L294:.RemoveNetworkFromXml  L314:.InsertCallInXml  L399:.SetRetainInXml  L407:.RetainOf  L413:.FindMember  L431:helpers de FlgNet  L433:.ParseParams  L446:.Access  L476:.Wire  L485:.Text  L500:.NextId …</sub>
- **BlockExplain.cs** (381 ln)
  <sub>L42:class BlockExplain  L44:.Explain  L84:.Val  L91:.Kids  L97:interface / membros de DB  L100:.Interface  L116:.Members  L125:.Member  L139:rede  L141:.Network  L160:.Text  L172:.Collapse  L173:.Cut  L175:FlgNet → expressão  L177:class Net  L362:rótulo do operando</sub>
- **BlockInterface.cs** (141 ln)
  <sub>L17:class Param  L23:.ToString  L32:class BlockInterface  L37:.Run  L88:.Collect  L94:núcleo puro (sem Openness: testável offline)  L97:.FromXml  L117:.Describe</sub>
- **Clone.cs** (233 ln)
  <sub>L24:class Clone  L26:.Run  L98:.InstancesInXml  L116:.Rewrite  L144:.Readdress  L195:.RewriteFile  L210:.ParseReplaces  L223:.ObjectName</sub>
- **DbMember.cs** (394 ln)
  <sub>L48:class DbMember  L57:.Add  L106:.Change  L152:.Remove  L182:coreografia comum: export → patch → Import Override → prova  L190:.ExportFresh  L202:.MemberOf  L214:.RemoveFromXml  L227:struct Delta  L235:.ChangeInXml  L273:struct Edit  L286:.AddToXml  L325:.ResolveSection  L360:.NameOf  L366:.Datatype  L374:.Safe  L379:.Report</sub>
- **Doctor.cs** (183 ln)
  <sub>L14:class Doctor  L23:.Run  L43:.CheckVerb  L67:case "gen-profinet"  L84:case "standardize-tags"  L93:case "gen-fault-ob"  L109:case "replicate-fc"  L126:case "gen-alarm-fc"  L144:case "replicate-instruments"  L175:.AnyFc</sub>
- **Drives.cs** (234 ln)
  <sub>L31:class Drives  L34:.DriveObjects  L41:.Collect  L61:.Try  L67:.Describe  L115:list-telegrams  L117:.ListTelegrams  L128:insert-telegram  L134:.InsertTelegram  L223:.ParseType</sub>
- **FaultOb.cs** (361 ln)
  <sub>L40:class FaultObConfig  L43:.GroupPrefix  L45:.Devices  L46:.TemplateOb  L47:.ObNamePrefix  L53:.AlarmDb  L54:.CommentCultures  L62:class FaultOb  L69:class Module  L76:.Generate  L156:.DiscoverTasks  L185:.AllDeviceGroups  L192:.WithSubGroups  L200:.CollectModules  L211:XML generation  L213:.BuildObXml  L243:.RewireNetwork  L280:.AddMasterCommentEntry  L309:.ReassignUids  L329:.ModuleType  L341:.WriteCsv</sub>
- **Hardware.cs** (819 ln)
  <sub>L57:class Hardware  L63:.FindDevice  L75:.HasItemNamed  L85:.Interface  L93:add-device  L96:.AddDevice  L121:delete-device  L123:.DeleteDevice  L136:plug-module  L144:.PlugModule  L230:.CollectSlots  L242:.FindItem  L255:.FindItem  L266:set-address  L268:.SetAddress  L297:set-io-address  L305:.SetIoAddress  L365:.CollectAddresses  L371:list-io-map  L381:.ListIoMap  L428:.ListIoMapRows  L440:.CollectMap  L468:.CollectTelegramMap  L495:.Range …</sub>
- **Hmi.cs** (43 ln)
  <sub>L10:class Hmi  L12:.Targets  L23:.List</sub>
- **InstrumentFc.cs** (724 ln)
  <sub>L54:class InstrumentFcConfig  L57:.SourceTagsFolder  L59:.TargetBlocksFolder  L60:.GlobalDb  L62:.TargetOb  L69:.FcSuffix  L70:.IgnoreFolders  L72:.TagFilters  L78:.MoldInstrumentId  L80:.NextCommandIds  L93:class InstrumentFc  L103:class Instrument  L116:class AreaTask  L124:.Run  L341:generation  L343:.BuildAreaFcXml  L371:.RewireNetwork  L425:.ImportAreaFc  L465:call OB  L467:.UpdateCallOb  L531:.CallNetworkXml  L562:.EmptyObXml  L592:checks + helpers  L595:.IsTaskComplete …</sub>
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
- **Replicate.cs** (582 ln)
  <sub>L47:class ReplicateFcConfig  L50:.BlocksFolder  L52:.EquipmentTypes  L54:.UdtNames  L56:.SourceNumbersToReplace  L57:.GlobalDb  L59:.StartNumber  L65:.TemplateFolder  L71:.TargetFolder  L79:class ReplicateFc  L81:.Run  L270:.TemplateFor  L282:.FindFolderByName  L295:.FoldersOfType  L304:.ReplicateInto  L352:.RewireXml  L447:.FindPathInDbXml  L481:naming  L483:.ExtractId  L489:.InstanceDbNames  L494:.ProposedBlockName  L507:.MainBlockName  L514:.FolderBaseName  L522:.static …</sub>
- **Scaffold.cs** (350 ln)
  <sub>L51:class ScaffoldManifest  L54:.Source  L57:.Folders  L60:.TagFolders  L63:.Replace  L70:.Cpu  L72:.Items  L75:class ScaffoldItem  L77:.File  L80:.Folder  L84:class ScaffoldPlanItem  L97:class Scaffold  L100:.Rank  L114:.Plan  L145:.Merge  L155:.Apply  L164:.Run  L238:.CheckFamily  L252:.SameFamily  L259:.AlreadyInAnotherFolder  L267:.DeleteObject  L278:.FolderAction  L301:.ResolveBlockPath  L318:.ResolveTypePath …</sub>
- **Standardize.cs** (654 ln)
  <sub>L59:class StandardizeConfig  L61:.RootFolder  L64:.MemorySets  L72:.SetMapping  L83:.PrefixMappings  L102:.CommentMappings  L167:.AlarmOrder  L173:class PrefixMapping  L175:.Keyword  L176:.Prefix  L179:class TagTemplate  L186:class NaturalStringComparer  L188:.Compare  L215:class AlarmTagComparer  L220:.AlarmTagComparer  L225:.Compare  L233:.Key  L244:class AddressAllocator  L248:.CurrentByte  L249:.CurrentBit  L251:.AddressAllocator  L257:.Next  L283:class MemoryManager  L287:.MemoryManager …</sub>
- **TiaSession.cs** (231 ln)
  <sub>L12:class TiaSession  L14:.Portal  L15:.Project  L17:.TiaSession  L24:.PortalFilter  L30:.PickProcess  L61:.Describe  L67:.Attach  L82:lifecycle (open/save/close)  L89:.OpenProject  L119:.CreateProject  L145:.LocalProject  L154:.Save  L161:.CloseProject  L171:.AllDevices  L180:.CollectDevices  L188:.ExclusiveAccess  L194:.Plcs  L206:.GetPlc  L226:.Dispose</sub>

