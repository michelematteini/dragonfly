#include <gpasdk/ittnotify.h>
#include <msclr/marshal.h>

using namespace msclr::interop;

namespace DragonflyUtils 
{

	/// <summary>
	/// Helper class that wraps intel trace analyzer calls for debug purposes.
	/// </summary>
	public ref class DF_ITT 
	{
	private:
		marshal_context marshalContext;
		__itt_domain* domain;
		System::Collections::Generic::Dictionary<int, System::IntPtr> ^ taskNames;

	public:
		DF_ITT(System::String^ domainName)
		{
			domain = __itt_domain_create(marshalContext.marshal_as<const WCHAR*>(domainName));
			taskNames = gcnew System::Collections::Generic::Dictionary<int, System::IntPtr>();
		}

		void TaskBegin(System::String ^ taskName)
		{
			int taskNameHash = taskName->GetHashCode();
			System::IntPtr taskStringHandle;
			if (!taskNames->TryGetValue(taskNameHash, taskStringHandle))
			{
				__itt_string_handle * taskNameStr = __itt_string_handle_create(marshalContext.marshal_as<const WCHAR*>(taskName));
				taskStringHandle = System::IntPtr(reinterpret_cast<void*>(taskNameStr));
				taskNames[taskNameHash] = taskStringHandle;
			}
			__itt_task_begin(domain, __itt_null, __itt_null, reinterpret_cast<__itt_string_handle *>(taskStringHandle.ToPointer()));
		}

		void TaskEnd()
		{
			__itt_task_end(domain);
		}

	};
}