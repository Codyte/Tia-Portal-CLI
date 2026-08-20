# __navi__ · `src/Tia.Core/` — 28 files → code symbols / text NAV ranges
<!-- navindex · 2026-08-20 · DO NOT EDIT BY HAND; regen via navindex skill -->
↑ repo tree: [`../../__navi__.md`](../../__navi__.md)

- **AlarmFc.cs** (750 ln)
  <sub>L59:class AlarmFcConfig  L61:.TargetRootFolder  L62:.TemplateFc  L63:.TemplateFolder  L64:.ObTemplate  L65:.GlobalDb  L66:.AlarmTagsFolder  L67:.StartTagsFolder  L68:.MasterFb  L69:.CallObName  L70:.CallObNumber  L71:.IgnoreFolders  L78:.IncludeFolders  L80:.Structs  L91:class AlarmFc  L101:class TagRef  L107:.Generate  L340:FC XML  L342:.BuildFcXml  L379:.RewireWordNetwork  L488:call OB  L490:.BuildCallObXml  L540:global DB comments  L542:.WriteDbComments …</sub>
- **AssemblyInfo.cs** (3 ln)
- **Audit.cs** (456 ln)
  <sub>L49:class Audit  L75:.HasInverter  L82:.MissingCore  L90:.TagOf  L97:.CarriesTag  L104:.NormalizeArea  L120:.IsCallBlock  L130:.IsLooseScalar  L141:.RootMembers  L152:.Run  L257:.NonGraphicCalls  L277:.MisplacedCalls  L303:.DbGlobalCheck  L323:.FindGlobalDb  L337:.Skipped  L346:.CountTypes  L351:.CollectLanguages  L364:.LayerLeaks  L390:.IsLibrary  L397:.AreaConflicts  L424:.Check  L440:.CollectBlocks  L448:.CollectTables</sub>
- **BlockEdit.cs** (584 ln)
  <sub>L57:class BlockEdit  L75:delete-network  L82:.DeleteNetwork  L122:add-call  L135:.AddCall  L169:class CallRequest  L180:class Prepared  L188:.Describe  L202:.Prepare  L272:set-retain  L280:.SetRetain  L302:coreografia  L308:.Patch  L318:núcleo puro (sem Openness: testável offline)  L320:class CallSpec  L333:.StripTypePrefix  L343:.DeleteOrder  L348:.CountNetworks  L354:.RemoveNetworkFromXml  L374:.InsertCallInXml  L459:.SetRetainInXml  L467:.RetainOf  L473:.FindMember  L491:helpers de FlgNet …</sub>
- **BlockExplain.cs** (381 ln)
  <sub>L42:class BlockExplain  L44:.Explain  L84:.Val  L91:.Kids  L97:interface / membros de DB  L100:.Interface  L116:.Members  L125:.Member  L139:rede  L141:.Network  L160:.Text  L172:.Collapse  L173:.Cut  L175:FlgNet → expressão  L177:class Net  L362:rótulo do operando</sub>
- **BlockInterface.cs** (140 ln)
  <sub>L17:class Param  L23:.ToString  L32:class BlockInterface  L37:.Run  L87:.Collect  L93:núcleo puro (sem Openness: testável offline)  L96:.FromXml  L116:.Describe</sub>
- **Clone.cs** (233 ln)
  <sub>L24:class Clone  L26:.Run  L98:.InstancesInXml  L116:.Rewrite  L144:.Readdress  L195:.RewriteFile  L210:.ParseReplaces  L223:.ObjectName</sub>
- **DbMember.cs** (457 ln)
  <sub>L42:class DbMember  L52:class MemberSpec  L66:.ParseSpec  L92:.RejectDuplicates  L106:.Order  L115:.Add  L140:.Validate  L156:.Row  L174:.Rows  L193:.Change  L243:.Remove  L281:núcleo comum (o envelope mora em Ops.EditBlock)  L284:.MemberOf  L296:.RemoveFromXml  L309:struct Delta  L317:.ChangeInXml  L355:struct Edit  L368:.AddToXml  L407:.ResolveSection  L442:.NameOf  L448:.Datatype</sub>
- **Doctor.cs** (183 ln)
  <sub>L14:class Doctor  L23:.Run  L43:.CheckVerb  L67:case "gen-profinet"  L84:case "standardize-tags"  L93:case "gen-fault-ob"  L109:case "replicate-fc"  L126:case "gen-alarm-fc"  L144:case "replicate-instruments"  L175:.AnyFc</sub>
- **Drives.cs** (399 ln)
  <sub>L37:class Drives  L40:.DriveObjects  L47:.Collect  L67:.Try  L73:.Describe  L121:list-telegrams  L123:.ListTelegrams  L134:list-drive-params  L144:.ListParams  L203:set-drive-param  L214:.SetParam  L276:.OutOfRange  L285:.TryNumber  L293:insert-telegram  L299:.InsertTelegram  L388:.ParseType</sub>
- **FaultOb.cs** (360 ln)
  <sub>L40:class FaultObConfig  L43:.GroupPrefix  L45:.Devices  L46:.TemplateOb  L47:.ObNamePrefix  L53:.AlarmDb  L54:.CommentCultures  L62:class FaultOb  L69:class Module  L76:.Generate  L155:.DiscoverTasks  L184:.AllDeviceGroups  L191:.WithSubGroups  L199:.CollectModules  L210:XML generation  L212:.BuildObXml  L242:.RewireNetwork  L279:.AddMasterCommentEntry  L308:.ReassignUids  L328:.ModuleType  L340:.WriteCsv</sub>
- **Hardware.cs** (951 ln)
  <sub>L61:class Hardware  L67:.FindDevice  L79:.HasItemNamed  L93:.SingleInterface  L115:.CollectInterfaces  L127:.Interface  L135:add-device  L138:.AddDevice  L163:delete-device  L165:.DeleteDevice  L178:plug-module  L186:.PlugModule  L272:.CollectSlots  L290:.FindItem  L309:.CollectMatches  L323:set-address  L325:.SetAddress  L358:set-io-address  L366:.SetIoAddress  L426:.CollectAddresses  L432:list-io-map  L442:.ListIoMap  L494:.ListIoMapRows  L507:.CollectMap …</sub>
- **Hmi.cs** (476 ln)
  <sub>L53:class Hmi  L56:.Targets  L72:.ExportTagTable  L97:.ImportTagTable  L129:.ResolveTagFolder  L134:.ResolveTagFolder  L154:.List  L168:.Describe  L209:.Tree  L261:.SplitPath  L271:.Row  L278:roundtrip SimaticML de tela (só clássico)  L281:.ExportScreen  L298:.ImportScreen  L344:.StripScreenNumber  L361:.DeleteScreen  L374:.FindScreen  L390:.ScreenPaths  L398:.TagNames  L405:.CollectTagNames  L415:.ClassicTarget  L434:.ResolveScreenFolder  L456:.CollectScreens  L465:.CollectTables</sub>
- **InstrumentFc.cs** (721 ln)
  <sub>L54:class InstrumentFcConfig  L57:.SourceTagsFolder  L59:.TargetBlocksFolder  L60:.GlobalDb  L62:.TargetOb  L69:.FcSuffix  L70:.IgnoreFolders  L72:.TagFilters  L78:.MoldInstrumentId  L80:.NextCommandIds  L93:class InstrumentFc  L103:class Instrument  L116:class AreaTask  L124:.Run  L339:generation  L341:.BuildAreaFcXml  L369:.RewireNetwork  L423:.ImportAreaFc  L463:call OB  L465:.UpdateCallOb  L528:.CallNetworkXml  L559:.EmptyObXml  L589:checks + helpers  L592:.IsTaskComplete …</sub>
- **Inventory.cs** (576 ln)
  <sub>L61:class Inventory  L63:.Info  L80:.Devices  L97:.CollectDeviceItems  L119:.FolderMatches  L133:.Blocks  L164:.CollectBlocks  L187:.Tree  L223:.AppendGrouped  L235:.AppendTree  L256:.BlockLabel  L264:.TagTables  L287:.FindTagTable  L299:.CollectTagTables  L312:.Types  L319:.CollectTypes  L332:find  L335:.Find  L369:.FindInTagTables  L409:snapshot  L412:.Snapshot  L430:cross-references  L437:.ResolveSymbol  L451:.FindTag …</sub>
- **LadConverter.cs** (530 ln)
  <sub>L46:class LadConverter  L48:.Convert  L91:lexer  L93:class Tok  L99:.Lex  L143:AST  L145:class Node  L146:class Leaf  L147:class CmpN  L148:class Group  L149:class NotN  L150:class Operand  L153:class Stmt  L161:parser (recursive descent)  L163:class Parser  L304:normalize: push NOT down to leaves (De Morgan; comparators invert)  L306:.Normalize  L321:FlgNet emitter  L327:class Net  L334:class Emitter  L450:.EmitNetwork  L476:block XML assembly  L478:.BuildBlockXml  L516:.Hex …</sub>
- **Library.cs** (372 ln)
  <sub>L34:class Library  L36:.Open  L60:.Create  L87:.Retrieve  L121:.List  L136:.CollectMasterCopies  L149:.CollectTypes  L167:.ImportMasterCopy  L255:.AddMasterCopy  L313:.DeleteMasterCopy  L332:.ResolveLibFolder  L345:.FindMasterCopy  L363:.Collect</sub>
- **Memory.cs** (113 ln)
  <sub>L20:class Memory  L22:.FreeM  L47:.Occupied  L64:.Width  L85:.Gaps  L104:.CollectTags</sub>
- **Motion.cs** (86 ln)
  <sub>L27:class Motion  L31:.List  L43:.Collect  L67:.Parameters  L80:.Safe</sub>
- **Multiuser.cs** (110 ln)
  <sub>L14:class Multiuser  L21:.ListServerProjects  L66:.ResolveServer  L81:.Describe</sub>
- **Ops.cs** (1581 ln)
  <sub>L106:class Ops  L108:lookup  L110:.FindBlock  L120:.FindGroup  L135:.FindGroupByName  L153:.FindTagGroup  L168:.FindTagGroupByName  L181:.FindBlockIn  L194:.ResolveFolder  L206:.SplitPath  L230:.WalkFolders  L257:.FindTagTable  L270:.ResolveTagFolder  L278:.ResolveTypeFolder  L285:.FindType  L297:structure  L299:.CreateFolder  L331:.CreateFolders  L366:.DeleteFolder  L393:.TypeFolderAction  L414:.CountTypes  L419:.CountBlocks  L424:.CountTables  L433:.CreateInstanceDb …</sub>
- **Profinet.cs** (166 ln)
  <sub>L12:class ProfinetConfig  L14:.Devices  L15:.StartByte  L16:.TagFolder  L17:.TagTable  L20:class ProfinetMapping  L22:.Hardware  L23:.EquipmentTag  L24:.DeviceNumber  L28:class Profinet  L30:.Generate  L92:.TagName  L97:.FindTable  L103:.ResolveTable  L111:.FindIoDeviceNames  L130:.FindNetworkItem  L143:class BoolAddressAllocator  L148:.BoolAddressAllocator  L153:.Next  L161:.Skip</sub>
- **Replicate.cs** (586 ln)
  <sub>L47:class ReplicateFcConfig  L50:.BlocksFolder  L52:.EquipmentTypes  L54:.UdtNames  L56:.SourceNumbersToReplace  L57:.GlobalDb  L59:.StartNumber  L65:.TemplateFolder  L71:.TargetFolder  L79:class ReplicateFc  L81:.Run  L269:.TemplateFor  L281:.FindFolderByName  L294:.FoldersOfType  L303:.ReplicateInto  L352:.RewireXml  L447:.FindPathInDbXml  L481:naming  L483:.ExtractId  L489:.InstanceDbNames  L494:.ProposedBlockName  L507:.MainBlockName  L514:.FolderBaseName  L522:.static …</sub>
- **Scaffold.cs** (351 ln)
  <sub>L51:class ScaffoldManifest  L54:.Source  L57:.Folders  L60:.TagFolders  L63:.Replace  L70:.Cpu  L72:.Items  L75:class ScaffoldItem  L77:.File  L80:.Folder  L84:class ScaffoldPlanItem  L97:class Scaffold  L100:.Rank  L114:.Plan  L145:.Merge  L155:.Apply  L164:.Run  L238:.CheckFamily  L252:.SameFamily  L259:.AlreadyInAnotherFolder  L267:.DeleteObject  L279:.FolderAction  L302:.ResolveBlockPath  L319:.ResolveTypePath …</sub>
- **ScreenItems.cs** (677 ln)
  <sub>L64:class ScreenItems  L66:class Item  L77:núcleo puro (sem Openness, testável offline)  L80:.Parse  L108:.Groups  L138:.Patch  L166:case "x"  L167:case "y"  L168:case "w"  L169:case "h"  L185:.Remove  L212:.Rename  L250:.RenameFromTag  L299:.Group  L359:.CopyInto  L424:verbos  L427:.List  L460:.Audit  L514:.Set  L552:.Copy  L577:utilidades  L580:.ScreenElements  L590:.GroupOf  L596:.NameOf …</sub>
- **Sim.cs** (675 ln)
  <sub>L75:class Sim  L83:.Run  L280:.Diag  L343:.Watch  L388:.Try  L394:.RegisteredInstances  L404:.WaitReady  L426:.ValidateSteps  L439:case "write"  L440:case "read"  L441:case "wait"  L442:case "run"  L466:.Execute  L476:case "write"  L480:case "read"  L484:case "wait"  L488:case "run"  L492:case "stop"  L496:case "state"  L499:case "tags"  L527:.Write  L551:.ParseBool  L559:.Plain  L580:class Target …</sub>
- **Standardize.cs** (655 ln)
  <sub>L59:class StandardizeConfig  L61:.RootFolder  L64:.MemorySets  L72:.SetMapping  L83:.PrefixMappings  L102:.CommentMappings  L167:.AlarmOrder  L173:class PrefixMapping  L175:.Keyword  L176:.Prefix  L179:class TagTemplate  L186:class NaturalStringComparer  L188:.Compare  L215:class AlarmTagComparer  L220:.AlarmTagComparer  L225:.Compare  L233:.Key  L244:class AddressAllocator  L248:.CurrentByte  L249:.CurrentBit  L251:.AddressAllocator  L257:.Next  L283:class MemoryManager  L287:.MemoryManager …</sub>
- **TiaSession.cs** (231 ln)
  <sub>L12:class TiaSession  L14:.Portal  L15:.Project  L17:.TiaSession  L24:.PortalFilter  L30:.PickProcess  L61:.Describe  L67:.Attach  L82:lifecycle (open/save/close)  L89:.OpenProject  L119:.CreateProject  L145:.LocalProject  L154:.Save  L161:.CloseProject  L171:.AllDevices  L180:.CollectDevices  L188:.ExclusiveAccess  L194:.Plcs  L206:.GetPlc  L226:.Dispose</sub>
