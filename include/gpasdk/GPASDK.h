//--------------------------------------------------------------------------------------
// Copyright (c) 2016-2020 Intel Corporation
// All Rights Reserved
//
// Permission is granted to use, copy, distribute and prepare derivative works of this
// software for any purpose and without fee, provided, that the above copyright notice
// and this statement appear in all copies.  Intel makes no representations about the
// suitability of this software for any purpose.  THIS SOFTWARE IS PROVIDED "AS IS."
// INTEL SPECIFICALLY DISCLAIMS ALL WARRANTIES, EXPRESS OR IMPLIED, AND ALL LIABILITY,
// INCLUDING CONSEQUENTIAL AND OTHER INDIRECT DAMAGES, FOR THE USE OF THIS SOFTWARE,
// INCLUDING LIABILITY FOR INFRINGEMENT OF ANY PROPRIETARY RIGHTS, AND INCLUDING THE
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE.  Intel does not
// assume any responsibility for any errors which may appear in this software nor any
// responsibility to update it.
//
//--------------------------------------------------------------------------------------

#pragma once
#include <cstdint>

struct IUnknown;

struct IGPA
{
    //! Holds possible return codes
    enum Result
    {
        Ok,                 //!< Operation succeeded
        NotSupported,       //!< Function is not supported for current API or platform
        Failed              //!< Operation failed
    };

    //! Specifies metric description
    //! @see CreateCustomMetric
    struct MetricDescription
    {
        const char* metric_name;    //!< name of metric which will be displayed in the UI
        const char* units_name;     //!< name of metric's unit, e.g. ms/%/frame.
    };

    typedef uint32_t TMetricHandle;

    //! @brief Marks the start of a frame sequence region.
    //! BeginFrameSequenceRegion and EndFrameSequenceRegion are used to specify a region of frames
    //! which is shown in System Analyzer. 
    //! Can't be used to mark parts of frame.
    //! @param region_name name of region
    //! @return possible errors:
    //!         NotSupported if the method is not supported on the current OS / API
    //!         InvalidOperation if called before the previous region is closed via EndFrameSequenceRegion
    virtual Result BeginFrameSequenceRegion(const char* region_name) = 0;

    //! @brief Marks frame sequence region end.
    //! @see BeginFrameSequenceRegion
    //! @return possible errors:
    //!         NotSupported if the method is not supported on the current OS / API
    //!         InvalidOperation if called before region is opened via BeginFrameSequenceRegion
    virtual Result EndFrameSequenceRegion() = 0;

    //! @brief Triggers a frame capture
    //! This method triggers GPA frame capture from inside the application.
    //! Frame starting after the next Present() following this call will be captured and stored in GPA directory.
    //! @param name    captured frame name, if nullptr is passed the frame name will be generated automatically
    //! @return possible errors:
    //!         NotSupported if the method is not supported on the current OS / API
    virtual Result CaptureFrame(const char* name = nullptr) = 0;

    //! @brief Creates custom metric
    //! Application can register a new metric and its value will be shown in System Analyzer
    //! @see UpdateCustomeMetricValue
    //! @param description  metric description
    //! @param out_handle   created metric's handle - used for further metric actions
    //! @return possible errors:
    //!         NotSupported if the method is not supported on the current OS / API
    virtual Result CreateCustomMetric(const MetricDescription& description, TMetricHandle& out_handle) = 0;

    //! @brief Removes custom metric
    //! @param metric   metric handle to remove
    //! @return possible errors:
    //!         NotSupported if the method is not supported on the current OS / API
    //!         InvalidOperation if metric handle is invalid or has been already removed
    virtual Result RemoveCustomMetric(TMetricHandle metric) = 0;

    //! @brief Updates metric value shown in System Analyzer
    //! @param metric   metric handle for which the value is updated
    //! @param value    new metric value
    //! return possible errors:
    //!         InvalidOperation if metric handle is invalid or has been removed
    virtual Result UpdateCustomMetricValue(TMetricHandle metric, double value) = 0;

    // We can use CustomPresent to tell GPA frame delineation
    // Possible use:
    //     ComPtr<ID3D11Device> device;
    //     render_target_view->GetDevice(&device);
    //     ComPtr<ID3D11Resource> resource;
    //     render_target_view->GetResource(&resource);
    //     gpa_interface->CustomPresent(device.Get(), resource.Get());
    // Limitation:
    //     CustomPresent support for DX11 only
    //     QueryInterface for device should be success get ID3D11Device
    //     QueryInterface for resource should be success get ID3D11Texture2D
    virtual Result CustomPresent(IUnknown* device, IUnknown* resource = nullptr) = 0;
};

extern "C"
{
    IGPA* GetGPAInterface();
}
