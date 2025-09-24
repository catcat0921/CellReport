<template>
 <el-dialog  v-draggable style="text-align: left;" v-if="dialogVisible"
    :visible.sync="dialogVisible" :title="'页面设置'" mini
        :close-on-click-modal="false"  @close="close" 
          direction="btt" append-to-body  
    >
    <el-form>
    <el-tabs value="first" @tab-click="handleClick">
        <el-tab-pane label="页面" name="first">
            <el-form label-width="80px">
                    <el-form-item label="纸张大小">
                        <el-select v-model="settings.pageSize_name" placeholder="请选择">
                            <el-option
                            v-for="item in pageSize_list"
                            :key="item.name"
                            :label="item.name"
                            :value="item.name">
                            </el-option>
                        </el-select>    
                    </el-form-item>

                    <el-form-item label="方向">
                        <el-radio v-model="settings.orientation" label="portrait">纵向(较长)</el-radio>
                        <el-radio v-model="settings.orientation" label="landscape">横向(较宽)</el-radio>
                    </el-form-item>

                    <el-form-item label="缩放">
                        <el-radio v-model="settings.fitToPage" :label="false">调整为</el-radio>
                        <el-radio v-model="settings.fitToPage" :label="true">缩放比例</el-radio>
                        
                    </el-form-item>
                    <el-form-item label="缩放比例" v-if="!settings.fitToPage">
                        <el-input-number v-model="settings.scale" :min=1></el-input-number>
                    </el-form-item> 
                    <el-form-item label="几页宽" v-if="settings.fitToPage">
                        <el-input-number v-model="settings.fitToWidth" :min=1></el-input-number>
                    </el-form-item>
                    <el-form-item label="几页高" v-if="settings.fitToPage">
                        <el-input-number v-model="settings.fitToHeight" :min=1></el-input-number>
                    </el-form-item> 
                    
            </el-form>
        </el-tab-pane>
    <el-tab-pane label="页边距" name="second">
        <el-form label-width="80px">
            <el-form-item label="上">
                <el-input-number v-model="settings.margin_top"></el-input-number>
            </el-form-item>
            <el-form-item label="下">
                <el-input-number v-model="settings.margin_bottom"></el-input-number>
            </el-form-item>
            <el-form-item label="左">
                <el-input-number v-model="settings.margin_left"></el-input-number>
            </el-form-item>
            <el-form-item label="右">
                <el-input-number v-model="settings.margin_right"></el-input-number>
            </el-form-item>
            <el-form-item label="页眉">
                <el-input-number v-model="settings.margin_header"></el-input-number>
            </el-form-item>
            <el-form-item label="页脚">
                <el-input-number v-model="settings.margin_footer"></el-input-number>
            </el-form-item>
            <el-form-item label="水平居中">
                <el-checkbox v-model="settings.horizontalCentered">水平居中</el-checkbox>
            </el-form-item>
            <el-form-item label="垂直居中">
                <el-checkbox v-model="settings.verticalCentered">垂直居中</el-checkbox>
            </el-form-item>            
        </el-form>
    </el-tab-pane>
    <el-tab-pane label="页眉/页脚" name="third">
        <el-form label-width="80px">
            <el-form-item label="页眉左">
                <el-input v-model="settings.header_left"></el-input>
            </el-form-item>
            <el-form-item label="页眉中">
                <el-input v-model="settings.header_center"></el-input>
            </el-form-item>
            <el-form-item label="页眉右">
                <el-input v-model="settings.header_right"></el-input>
            </el-form-item>
            <el-form-item label="页脚左">
                <el-input v-model="settings.footer_left"></el-input>
            </el-form-item>
            <el-form-item label="页脚中">
                <el-input v-model="settings.footer_center"></el-input>
            </el-form-item>
            <el-form-item label="页脚右">
                <el-input v-model="settings.footer_right"></el-input>
            </el-form-item>                 
        </el-form>
    </el-tab-pane>
    <el-tab-pane label="工作表" name="fourth">
        <el-form-item label="套打背景图">
                <el-input v-model="settings.print_template_background"></el-input>
            </el-form-item>
    </el-tab-pane>
  </el-tabs>
</el-form>
    <div slot="footer" class="dialog-footer">
          <el-button @click="dialogVisible = false">取 消</el-button>
          <el-button type="primary" @click="handleSubmit">确 定</el-button>
    </div>
</el-dialog>
</template>

<script>
export default {
    name: 'paperSetting',  
    props: { visible: Boolean, target_obj: Object,},
    mounted(){
        this.settings=Object.assign({}, this.settings,this.target_obj)
        this.set_pageSize_name();
        
    },
    data(){
        return {
            settings:{
                pageSize_name:'A4',
                pageSize_Width:595,
                pageSize_Height:842,
                orientation:'portrait',
                fitToPage:true,
                scale:100,
                fitToWidth:'1',
                fitToHeight:'1000',
                margin_top:36,
                margin_bottom:36,
                margin_left:36,
                margin_right:36,
                page_header:10,
                page_footer:10,
                horizontalCentered:false,
                verticalCentered:false,
                page_header_left:'',
                page_header_center:'',
                page_header_right:'',
                page_footer_left:'',
                page_footer_center:'',
                page_footer_right:'',
                print_template_background:''
            },
            dialogVisible:false,
            pageSize_list:[
                {   'name':'A0',value:[2384, 3370] },
                {   'name':'A1',value:[1684, 2384] },
                {   'name':'A2',value:[1190, 1684] },
                {   'name':'A3',value:[842, 1190] },
                {   'name':'A4',value:[595, 842] },
                {   'name':'A5',value:[420, 595] },
                {   'name':'A6',value:[298, 420] },
                {   'name':'A7',value:[210, 298] },
                {   'name':'A8',value:[148, 210] },
                {   'name':'A9',value:[105, 547] },
                {   'name':'A10',value:[74, 105] },
                {   'name':'B0',value:[2834, 4008] },
                {   'name':'B1',value:[2004, 2834] },
                {   'name':'B2',value:[1417, 2004] },
                {   'name':'B3',value:[1000, 1417] },
                {   'name':'B4',value:[708, 1000] },
                {   'name':'B5',value:[498, 708] },
                {   'name':'B6',value:[354, 498] },
                {   'name':'B7',value:[249, 354] },
                {   'name':'B8',value:[175, 249] },
                {   'name':'B9',value:[124, 175] },
                {   'name':'B10',value:[88, 124] },
                {   'name':'EXECUTIVE',value:[522, 756] },
                {   'name':'LEDGER',value:[1224, 792] },
                {   'name':'LEGAL',value:[612, 1008] },
                {   'name':'LETTER',value:[612, 792] },
                {   'name':'TABLOID',value:[792, 1224] },
            ],

        }
    },
    watch: {
        "settings.pageSize_name"(){
            this.set_pageSize_name()
        },
        "settings.orientation"(){
                let t;
                t=this.settings.pageSize_Width
                this.settings.pageSize_Width=this.settings.pageSize_Height
                this.settings.pageSize_Height=t            
        },
        dialogVisible(val) {
            this.$emit('update:visible', val)
        },
        visible(val) {
            this.dialogVisible=val
            this.$emit('update:visible', val)
        }
    },
    methods:{
        handleSubmit(){
            this.$emit('update:visible', false)
            this.$emit("submit",this.settings)        
        },
        set_pageSize_name(){
            let cur=this.pageSize_list.filter(x=>x.name==this.settings.pageSize_name)[0]
            this.settings.pageSize_Width=cur.value[0];
            this.settings.pageSize_Height=cur.value[1];
            if(this.settings.orientation!='portrait'){
                let t=this.settings.pageSize_Width
                this.settings.pageSize_Width=this.settings.pageSize_Height
                this.settings.pageSize_Height=t
            }
            //console.info(JSON.stringify( this.settings))
        }

    }
}
</script>

<style>

</style>