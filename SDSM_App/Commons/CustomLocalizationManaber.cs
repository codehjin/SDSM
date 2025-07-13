using Telerik.Windows.Controls;

namespace SDSM_App.Commons;

public class CustomLocalizationManaber : LocalizationManager
{
    public override string GetStringOverride(string key)
    {
        switch (key)
        {
            case "OK":
                return "확인";
            case "Cancel":
                return "취소";
            case "RadDataPagerPage":
                return "페이지";
            case "RadDataPagerOf":
                return "/";
            case "Close":
                return "닫기";
            case "EnterDate":
                return "0000-00-00";
            case "Today":
                return "오늘";
            case "Error":
                return "오류";
            case "GridViewAlwaysVisibleNewRow":
                return "새 항목을 추가하려면 여기를 클릭하세요";
            case "GridViewClearFilter":
                return "필터 지우기";
            case "GridViewFilter":
                return "필터";
            case "GridViewFilterAnd":
                return "그리고";
            case "GridViewFilterOr":
                return "또는";
            case "GridViewFilterContains":
                return "포함";
            case "GridViewFilterDoesNotContain":
                return "포함하지 않음";
            case "GridViewFilterStartsWith":
                return "시작 문자";
            case "GridViewFilterEndsWith":
                return "끝 문자";
            case "GridViewFilterIsContainedIn":
                return "에 포함";
            case "GridViewFilterIsEqualTo":
                return "같음";
            case "GridViewFilterIsNotEqualTo":
                return "같지 않음";
            case "GridViewFilterIsGreaterThan":
                return "보다 큼";
            case "GridViewFilterIsGreaterThanOrEqualTo":
                return "크거나 같음";
            case "GridViewFilterIsLessThan":
                return "보다 작음";
            case "GridViewFilterIsLessThanOrEqualTo":
                return "작거나 같음";
            case "GridViewFilterIsNotContainedIn":
                return "에 포함하지 않음";
            case "GridViewFilterIsNull":
                return "Null";
            case "GridViewFilterIsNotNull":
                return "Not Null";
            case "GridViewFilterIsEmpty":
                return "Empty";
            case "GridViewFilterIsNotEmpty":
                return "Not Empty";
            case "GridViewFilterMatchCase":
                return "대소문자 구분";
            case "GridViewFilterSelectAll":
                return "전체선택(최대 1000건)";
            case "GridViewFilterShowRowsWithValueThat":
                return "다음 값을 가진 행 표시";
            case "GridViewFilterDistinctValueNull":
                return "(빈값-Null)";
            case "GridViewFilterDistinctValueStringEmpty":
                return "(빈값-Empty)";
            case "GridViewGroupPanelText":
                return "열 머리글을 끌어 여기에 놓으면 해당 열별로 그룹화됩니다";
            case "GridViewGroupPanelTopText":
                return "그룹 머리글";
            case "GridViewGroupPanelTopTextGrouped":
                return "그룹";
            case "GridViewSearchPanelTopText":
                return "검색";
            case "GridViewColumnsSelectionButtonTooltip":
                return "열 선택";
            case "GridViewFilterDistinctValueSearch":
                return "검색어";
            case "Hide":
                return "닫기";
            case "Auto_hide":
                return "자동 숨기기";
            case "Floating":
                return "부동";
            case "Dockable":
                return "도킹";
            case "Tabbed_document":
                return "탭 문서로 도킹";
            case "Docking_ActivePanes":
                return "창 활성화";
            case "Docking_ActiveDocuments":
                return "활성 문서";
            case "Docking_PreviewHeader":
                return "미리보기";
            case "DataForm_MoveCurrentToFirst":
                return "처음으로";
            case "DataForm_MoveCurrentToPrevious":
                return "이전";
            case "DataForm_MoveCurrentToNext":
                return "다음";
            case "DataForm_MoveCurrentToLast":
                return "끝으로";
            case "DataForm_AddNew":
                return "추가";
            case "DataForm_BeginEdit":
                return "수정";
            case "DataForm_Delete":
                return "삭제";
            case "DataForm_OK":
                return "확인";
            case "Maximize":
                return "최대화";
            case "Restore":
                return "이전크기로 복원";
            case "Minimize":
                return "최소화";
            case "ChartViewNoAngleAxis":
                return "각도 축이 설정되지 않음";
            case "ChartViewNoData":
                return "자료가 없습니다.";
            case "ChartViewNoHorizontalAxis":
                return "수평 축이 설정되지 않음";
            case "ChartViewNoPolarAxis":
                return "극지 축이 설정되지 않음";
            case "ChartViewNoSeries":
                return "시리즈가 없음";
            case "ChartViewNoVerticalAxis":
                return "수직 축이 설정되지 않음";
            case "BusyIndicatorLoading":
                return "로딩 중...";
            case "FixedDocumentViewers_BeginningOfDocumentReachedMessage":
                return "문서의 시작 부분";
            case "FixedDocumentViewers_CaseSensitive":
                return "대소문자 구분";
            case "FixedDocumentViewers_EndOfDocumentReachedMessage":
                return "문서의 끝 구분";
            case "FixedDocumentViewers_FindDialogHeader":
                return "찾기";
            case "FixedDocumentViewers_FindNext":
                return "다음 찾기";
            case "FixedDocumentViewers_FindPrevious":
                return "이전 찾기";
            case "FixedDocumentViewers_Options":
                return "옵션";
            case "FixedDocumentViewers_SearchInTheDocument":
                return "문서 검색";
            case "FixedDocumentViewers_UseRegularExpression":
                return "정규식 사용";
            case "FixedDocumentViewers_WholeWordsOnly":
                return "일치";
            case "FixedDocumentViewers_FitWidth":
                return "창 너비에 맞춤";
            case "FixedDocumentViewers_ZoomToPageLevel":
                return "창 높이에 맞춤";
            case "FixedDocumentViewers_Print":
                return "인쇄";
            case "FilterAnd": return "그리고";
            case "FilterContains": return "포함";
            case "FilterEndsWith": return "다음으로 끝남";
            case "FilterIsContainedIn": return "포함됨";
            case "FilterIsEqualTo": return "같음";
            case "FilterIsGreaterThan": return "크다";
            case "FilterIsGreaterThanOrEqualTo": return "크거나 같음";
            case "FilterIsLessThan": return "다음보다 작음";

        }
        return base.GetStringOverride(key);
    }
}
